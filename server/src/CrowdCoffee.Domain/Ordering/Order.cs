namespace CrowdCoffee.Domain.Ordering;

/// <summary>
/// Schema only for now (Epic 2). Public creation (cash vs. card/wallet entry points, §7) and
/// transition behavior are added in Epic 3 — see ARCHITECTURE.md and PRD §15 instruction #5
/// ("build the domain model and order state machine first").
/// </summary>
public class Order
{
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

    private readonly List<OrderStatusHistory> _statusHistory = [];
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory;

    private Order()
    {
    }
}
