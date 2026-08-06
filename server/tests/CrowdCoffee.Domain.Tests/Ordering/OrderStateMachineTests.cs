using CrowdCoffee.Domain.Ordering;

namespace CrowdCoffee.Domain.Tests.Ordering;

public class OrderStateMachineTests
{
    private static readonly DateTime Now = new(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);

    private static Order CreateCashOrder(OrderMode mode = OrderMode.Pickup) =>
        Order.CreateCash(Guid.NewGuid(), "CRW-260725-0001", "Alaa", "01000000000", mode,
            mode == OrderMode.Delivery ? "123 Corniche St" : null, null, null, 100m, mode == OrderMode.Delivery ? 30m : 0m,
            Guid.NewGuid().ToString(), Now);

    private static Order CreatePendingPaymentOrder(OrderMode mode = OrderMode.Delivery, PaymentMethod method = PaymentMethod.Card) =>
        Order.CreatePendingPayment(Guid.NewGuid(), "CRW-260725-0002", "Alaa", "01000000000", mode,
            mode == OrderMode.Delivery ? "123 Corniche St" : null, null, null, 100m, mode == OrderMode.Delivery ? 30m : 0m,
            method, Guid.NewGuid().ToString(), Now);

    // --- Entry points (Story 3.2) ---

    [Fact]
    public void CreateCash_StartsAtPending()
    {
        var order = CreateCashOrder();

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(PaymentMethod.Cash, order.PaymentMethod);
    }

    [Fact]
    public void CreatePendingPayment_StartsAtPendingPayment()
    {
        var order = CreatePendingPaymentOrder();

        Assert.Equal(OrderStatus.PendingPayment, order.Status);
    }

    [Theory]
    [InlineData(PaymentMethod.Card)]
    [InlineData(PaymentMethod.Wallet)]
    public void CreatePendingPayment_AcceptsCardOrWallet(PaymentMethod method)
    {
        var order = CreatePendingPaymentOrder(method: method);

        Assert.Equal(method, order.PaymentMethod);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
    }

    [Fact]
    public void CreatePendingPayment_RejectsCash()
    {
        Assert.Throws<InvalidOperationException>(() => CreatePendingPaymentOrder(method: PaymentMethod.Cash));
    }

    // --- Legal transitions (Story 3.1) ---

    [Fact]
    public void CashOrder_Pending_To_Confirmed_Succeeds()
    {
        var order = CreateCashOrder();

        var result = order.TransitionTo(OrderStatus.Confirmed, "barista1", Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(Now, order.ConfirmedAt);
    }

    [Fact]
    public void CardOrder_PendingPayment_To_Confirmed_Succeeds()
    {
        var order = CreatePendingPaymentOrder();

        var result = order.TransitionTo(OrderStatus.Confirmed, "paymob-webhook", Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void FullHappyPath_Delivery_ReachesDelivered()
    {
        var order = CreateCashOrder(OrderMode.Delivery);

        Assert.True(order.TransitionTo(OrderStatus.Confirmed, "staff", Now).IsSuccess);
        Assert.True(order.TransitionTo(OrderStatus.Preparing, "staff", Now).IsSuccess);
        Assert.True(order.TransitionTo(OrderStatus.Ready, "staff", Now).IsSuccess);
        Assert.True(order.TransitionTo(OrderStatus.OutForDelivery, "staff", Now).IsSuccess);
        Assert.True(order.TransitionTo(OrderStatus.Delivered, "staff", Now).IsSuccess);

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(Now, order.CompletedAt);
    }

    [Fact]
    public void FullHappyPath_Pickup_ReachesPickedUp()
    {
        var order = CreateCashOrder(OrderMode.Pickup);

        Assert.True(order.TransitionTo(OrderStatus.Confirmed, "staff", Now).IsSuccess);
        Assert.True(order.TransitionTo(OrderStatus.Preparing, "staff", Now).IsSuccess);
        Assert.True(order.TransitionTo(OrderStatus.Ready, "staff", Now).IsSuccess);
        Assert.True(order.TransitionTo(OrderStatus.PickedUp, "staff", Now).IsSuccess);

        Assert.Equal(OrderStatus.PickedUp, order.Status);
        Assert.Equal(Now, order.CompletedAt);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.PendingPayment)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Preparing)]
    public void Cancellation_AllowedFromAnyStateBeforeReady(OrderStatus fromStatus)
    {
        var order = fromStatus == OrderStatus.PendingPayment ? CreatePendingPaymentOrder() : CreateCashOrder();
        AdvanceTo(order, fromStatus);

        var result = order.TransitionTo(OrderStatus.Cancelled, "staff", Now, "customer request");

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    // --- Illegal transitions (Story 3.1) ---

    [Theory]
    [InlineData(OrderStatus.Ready)]
    [InlineData(OrderStatus.OutForDelivery)]
    [InlineData(OrderStatus.Delivered)]
    public void Cancellation_RejectedFromReadyOrLater(OrderStatus fromStatus)
    {
        var order = CreateCashOrder(OrderMode.Delivery);
        AdvanceTo(order, fromStatus);

        var result = order.TransitionTo(OrderStatus.Cancelled, "staff", Now);

        Assert.True(result.IsFailure);
        Assert.Equal(fromStatus, order.Status);
    }

    [Fact]
    public void Ready_To_Pending_IsRejected()
    {
        var order = CreateCashOrder();
        AdvanceTo(order, OrderStatus.Ready);

        var result = order.TransitionTo(OrderStatus.Pending, "staff", Now);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderStatus.Ready, order.Status);
    }

    [Fact]
    public void Pending_To_Preparing_SkipsConfirmed_IsRejected()
    {
        var order = CreateCashOrder();

        var result = order.TransitionTo(OrderStatus.Preparing, "staff", Now);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void TerminalState_Delivered_AcceptsNoFurtherTransitions()
    {
        var order = CreateCashOrder(OrderMode.Delivery);
        AdvanceTo(order, OrderStatus.Delivered);

        var result = order.TransitionTo(OrderStatus.Cancelled, "staff", Now);

        Assert.True(result.IsFailure);
    }

    // --- Mode guard ---

    [Fact]
    public void Ready_To_OutForDelivery_RejectedForPickupOrder()
    {
        var order = CreateCashOrder(OrderMode.Pickup);
        AdvanceTo(order, OrderStatus.Ready);

        var result = order.TransitionTo(OrderStatus.OutForDelivery, "staff", Now);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderStatus.Ready, order.Status);
    }

    [Fact]
    public void Ready_To_PickedUp_RejectedForDeliveryOrder()
    {
        var order = CreateCashOrder(OrderMode.Delivery);
        AdvanceTo(order, OrderStatus.Ready);

        var result = order.TransitionTo(OrderStatus.PickedUp, "staff", Now);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderStatus.Ready, order.Status);
    }

    // --- History + events (Story 3.1 / 3.3) ---

    [Fact]
    public void EveryTransition_AppendsStatusHistory()
    {
        var order = CreateCashOrder();

        order.TransitionTo(OrderStatus.Confirmed, "staff", Now, "confirmed by phone");

        var entry = Assert.Single(order.StatusHistory);
        Assert.Equal(OrderStatus.Pending, entry.FromStatus);
        Assert.Equal(OrderStatus.Confirmed, entry.ToStatus);
        Assert.Equal("staff", entry.ChangedBy);
        Assert.Equal("confirmed by phone", entry.Reason);
    }

    [Fact]
    public void IllegalTransition_DoesNotAppendStatusHistory()
    {
        var order = CreateCashOrder();

        order.TransitionTo(OrderStatus.Preparing, "staff", Now);

        Assert.Empty(order.StatusHistory);
    }

    [Fact]
    public void Cancellation_FromConfirmed_RaisesOrderCancelledDomainEvent()
    {
        // Only Confirmed (or later, pre-Ready) states ever had stock reserved (§8) — this is the
        // case the release-stock handler exists for.
        var order = CreateCashOrder();
        AdvanceTo(order, OrderStatus.Confirmed);
        order.ClearDomainEvents(); // discard the Confirmed step's own OrderStatusChangedEvent

        order.TransitionTo(OrderStatus.Cancelled, "staff", Now);

        var cancelled = Assert.Single(order.DomainEvents.OfType<OrderCancelledEvent>());
        Assert.Equal(order.Id, cancelled.OrderId);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.PendingPayment)]
    public void Cancellation_FromPendingOrPendingPayment_RaisesNoOrderCancelledEvent(OrderStatus fromStatus)
    {
        // Nothing was ever reserved for an order still awaiting confirmation/payment — raising
        // OrderCancelledEvent here would make the release handler claw back stock this order
        // never held (see the comment on Order.TransitionTo). The kitchen-board-facing
        // OrderStatusChangedEvent still fires (Epic 12 needs to know this order left the board).
        var order = fromStatus == OrderStatus.PendingPayment ? CreatePendingPaymentOrder() : CreateCashOrder();

        order.TransitionTo(OrderStatus.Cancelled, "staff", Now);

        Assert.Empty(order.DomainEvents.OfType<OrderCancelledEvent>());
        var statusChanged = Assert.Single(order.DomainEvents.OfType<OrderStatusChangedEvent>());
        Assert.Equal(OrderStatus.Cancelled, statusChanged.NewStatus);
    }

    [Fact]
    public void NonCancellingTransition_RaisesNoOrderCancelledOrCompletedEvent()
    {
        var order = CreateCashOrder();

        order.TransitionTo(OrderStatus.Confirmed, "staff", Now);

        Assert.Empty(order.DomainEvents.OfType<OrderCancelledEvent>());
        Assert.Empty(order.DomainEvents.OfType<OrderCompletedEvent>());
    }

    [Fact]
    public void EveryTransition_RaisesOrderStatusChangedEvent_WithTheNewStatus()
    {
        var order = CreateCashOrder();

        order.TransitionTo(OrderStatus.Confirmed, "staff", Now);

        var statusChanged = Assert.Single(order.DomainEvents.OfType<OrderStatusChangedEvent>());
        Assert.Equal(order.Id, statusChanged.OrderId);
        Assert.Equal(OrderStatus.Confirmed, statusChanged.NewStatus);
    }

    [Fact]
    public void IllegalTransition_RaisesNoOrderStatusChangedEvent()
    {
        var order = CreateCashOrder();

        order.TransitionTo(OrderStatus.Preparing, "staff", Now); // Pending -> Preparing skips Confirmed

        Assert.Empty(order.DomainEvents);
    }

    [Fact]
    public void ClearDomainEvents_EmptiesTheCollection()
    {
        var order = CreateCashOrder();
        AdvanceTo(order, OrderStatus.Confirmed);
        order.TransitionTo(OrderStatus.Cancelled, "staff", Now);
        Assert.NotEmpty(order.DomainEvents); // sanity: there's actually something to clear

        order.ClearDomainEvents();

        Assert.Empty(order.DomainEvents);
    }

    /// Test-only helper: walks the happy path far enough to reach `target`, for setting up
    /// "cancel/transition from state X" scenarios without repeating the walk in every test.
    private static void AdvanceTo(Order order, OrderStatus target)
    {
        List<OrderStatus> path = [OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Ready];
        path.AddRange(order.Mode == OrderMode.Delivery
            ? [OrderStatus.OutForDelivery, OrderStatus.Delivered]
            : [OrderStatus.PickedUp]);

        foreach (var step in path)
        {
            if (order.Status == target)
            {
                return;
            }

            var result = order.TransitionTo(step, "staff", Now);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Test setup failed advancing {order.Status} -> {step}: {result.Error}");
            }
        }
    }
}
