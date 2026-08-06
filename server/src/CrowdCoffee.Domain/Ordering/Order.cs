using CrowdCoffee.Domain.Common;

namespace CrowdCoffee.Domain.Ordering;

/// <summary>
/// Order lifecycle per §7. Construction only through CreateCash/CreatePendingPayment — this
/// enforces §7's rule that cash orders start at Pending and card/wallet orders start at
/// PendingPayment and can never skip straight to Confirmed, bypassing gateway webhook
/// confirmation (there is no public constructor).
/// </summary>
public class Order : IHasDomainEvents
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.PendingPayment] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Preparing, OrderStatus.Cancelled],
        [OrderStatus.Preparing] = [OrderStatus.Ready, OrderStatus.Cancelled],
        [OrderStatus.Ready] = [OrderStatus.OutForDelivery, OrderStatus.PickedUp],
        [OrderStatus.OutForDelivery] = [OrderStatus.Delivered],
        [OrderStatus.PickedUp] = [],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = []
    };

    public Guid Id { get; private set; }

    /// Human-readable, separate from Id — CRW-YYMMDD-NNNN (§6 rule 2). Generated in Epic 9.
    public string OrderNumber { get; private set; } = string.Empty;

    public string CustomerName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public OrderMode Mode { get; private set; }
    public string? Address { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    public decimal Subtotal { get; private set; }
    public decimal DeliveryFee { get; private set; }
    public decimal Total { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public OrderStatus Status { get; private set; }

    public string IdempotencyKey { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items;

    /// Epic 9 checkout: appends a priced/snapshotted line while the order is still being
    /// assembled by CreateOrderHandler, before the first SaveChangesAsync.
    public void AddItem(OrderItem item) => _items.Add(item);

    /// Epic 10.3: set from the gateway webhook once its HMAC has been verified — never from an
    /// unauthenticated client request. Deliberately independent of Status/TransitionTo: a payment
    /// can be marked Paid or Failed without that alone deciding the order's lifecycle state
    /// (the webhook handler calls TransitionTo separately for that).
    public void MarkPaymentPaid() => PaymentStatus = PaymentStatus.Paid;

    public void MarkPaymentFailed() => PaymentStatus = PaymentStatus.Failed;

    private readonly List<OrderStatusHistory> _statusHistory = [];
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory;

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;
    public void ClearDomainEvents() => _domainEvents.Clear();

    private Order()
    {
    }

    public static Order CreateCash(
        Guid id, string orderNumber, string customerName, string phone, OrderMode mode,
        string? address, double? latitude, double? longitude,
        decimal subtotal, decimal deliveryFee, string idempotencyKey, DateTime createdAtUtc, string? notes = null) =>
        new()
        {
            Id = id,
            OrderNumber = orderNumber,
            CustomerName = customerName,
            Phone = phone,
            Mode = mode,
            Address = address,
            Latitude = latitude,
            Longitude = longitude,
            Subtotal = subtotal,
            DeliveryFee = deliveryFee,
            Total = subtotal + deliveryFee,
            PaymentMethod = PaymentMethod.Cash,
            PaymentStatus = PaymentStatus.Pending,
            Status = OrderStatus.Pending,
            IdempotencyKey = idempotencyKey,
            CreatedAt = createdAtUtc,
            Notes = notes
        };

    public static Order CreatePendingPayment(
        Guid id, string orderNumber, string customerName, string phone, OrderMode mode,
        string? address, double? latitude, double? longitude,
        decimal subtotal, decimal deliveryFee, PaymentMethod paymentMethod, string idempotencyKey,
        DateTime createdAtUtc, string? notes = null)
    {
        if (paymentMethod == PaymentMethod.Cash)
        {
            throw new InvalidOperationException("Cash orders must be created via CreateCash, not CreatePendingPayment.");
        }

        return new Order
        {
            Id = id,
            OrderNumber = orderNumber,
            CustomerName = customerName,
            Phone = phone,
            Mode = mode,
            Address = address,
            Latitude = latitude,
            Longitude = longitude,
            Subtotal = subtotal,
            DeliveryFee = deliveryFee,
            Total = subtotal + deliveryFee,
            PaymentMethod = paymentMethod,
            PaymentStatus = PaymentStatus.Pending,
            Status = OrderStatus.PendingPayment,
            IdempotencyKey = idempotencyKey,
            CreatedAt = createdAtUtc,
            Notes = notes
        };
    }

    /// <summary>
    /// Enforces the §7 transition graph plus the delivery/pickup mode guard (Ready only ever
    /// splits toward OutForDelivery for Delivery orders or PickedUp for Pickup orders — the
    /// diagram's two branches aren't both legal for every order). Every successful call appends
    /// an OrderStatusHistory row in the same operation (§7: "no exceptions").
    /// </summary>
    public Result TransitionTo(OrderStatus newStatus, string changedBy, DateTime occurredAtUtc, string? reason = null)
    {
        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
        {
            return Result.Failure(Error.Conflict(
                "Order.IllegalTransition",
                $"Cannot transition order from {Status} to {newStatus}."));
        }

        if (newStatus == OrderStatus.OutForDelivery && Mode != OrderMode.Delivery)
        {
            return Result.Failure(Error.Conflict(
                "Order.ModeMismatch",
                "Only delivery orders can be marked out for delivery."));
        }

        if (newStatus == OrderStatus.PickedUp && Mode != OrderMode.Pickup)
        {
            return Result.Failure(Error.Conflict(
                "Order.ModeMismatch",
                "Only pickup orders can be marked picked up."));
        }

        var previousStatus = Status;
        Status = newStatus;

        if (newStatus == OrderStatus.Confirmed)
        {
            ConfirmedAt = occurredAtUtc;
        }

        if (newStatus is OrderStatus.Delivered or OrderStatus.PickedUp)
        {
            CompletedAt = occurredAtUtc;
        }

        _statusHistory.Add(new OrderStatusHistory(Guid.NewGuid(), Id, previousStatus, newStatus, changedBy, occurredAtUtc, reason));

        // §8: stock is only ever reserved on the Confirmed transition — an order cancelled while
        // still Pending/PendingPayment never had anything reserved. Raising OrderCancelledEvent
        // for those would make ReleaseStockOnOrderCancelledHandler "release" ingredient
        // quantities this order never held, incorrectly clawing back another order's legitimate
        // reservation on the same StockItem. Only Confirmed/Preparing passed through Confirmed.
        if (newStatus == OrderStatus.Cancelled && previousStatus is OrderStatus.Confirmed or OrderStatus.Preparing)
        {
            _domainEvents.Add(new OrderCancelledEvent(Id));
        }

        if (newStatus is OrderStatus.Delivered or OrderStatus.PickedUp)
        {
            _domainEvents.Add(new OrderCompletedEvent(Id));
        }

        return Result.Success();
    }
}
