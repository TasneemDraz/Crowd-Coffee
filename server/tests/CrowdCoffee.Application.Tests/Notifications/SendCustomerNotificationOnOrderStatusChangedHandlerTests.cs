using CrowdCoffee.Application.Notifications;
using CrowdCoffee.Application.Ordering;
using CrowdCoffee.Domain.Ordering;

namespace CrowdCoffee.Application.Tests.Notifications;

public class SendCustomerNotificationOnOrderStatusChangedHandlerTests
{
    private static readonly DateTime Now = new(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);

    private static Order CreateOrder() =>
        Order.CreateCash(Guid.NewGuid(), "CRW-260725-0001", "Alaa", "01000000000", OrderMode.Pickup,
            null, null, null, 100m, 0m, Guid.NewGuid().ToString(), Now);

    private class FakeOrderRepository(Order? order) : IOrderRepository
    {
        public Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken) => Task.FromResult(order);
        public Task<IReadOnlyList<Order>> GetStalePendingPaymentOrdersAsync(DateTime olderThanUtc, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<Order>> GetActiveKitchenOrdersAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<Order>> SearchOrdersAsync(OrderSearchFilters filters, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Order?> GetByIdWithHistoryAsync(Guid orderId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void Add(Order order) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private class FakeNotificationService : INotificationService
    {
        public int ConfirmedCalls { get; private set; }
        public int ReadyCalls { get; private set; }
        public int OutForDeliveryCalls { get; private set; }

        public Task NotifyOrderConfirmedAsync(Order order, CancellationToken cancellationToken)
        {
            ConfirmedCalls++;
            return Task.CompletedTask;
        }

        public Task NotifyOrderReadyAsync(Order order, CancellationToken cancellationToken)
        {
            ReadyCalls++;
            return Task.CompletedTask;
        }

        public Task NotifyOrderOutForDeliveryAsync(Order order, CancellationToken cancellationToken)
        {
            OutForDeliveryCalls++;
            return Task.CompletedTask;
        }
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Ready)]
    [InlineData(OrderStatus.OutForDelivery)]
    public async Task HandleAsync_NotifiableStatus_CallsMatchingNotification(OrderStatus status)
    {
        var order = CreateOrder();
        var notifications = new FakeNotificationService();
        var handler = new SendCustomerNotificationOnOrderStatusChangedHandler(notifications, new FakeOrderRepository(order));

        await handler.HandleAsync(new OrderStatusChangedEvent(order.Id, status), CancellationToken.None);

        var totalCalls = notifications.ConfirmedCalls + notifications.ReadyCalls + notifications.OutForDeliveryCalls;
        Assert.Equal(1, totalCalls);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.PendingPayment)]
    [InlineData(OrderStatus.Preparing)]
    [InlineData(OrderStatus.PickedUp)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task HandleAsync_NonNotifiableStatus_CallsNothing(OrderStatus status)
    {
        var order = CreateOrder();
        var notifications = new FakeNotificationService();
        var handler = new SendCustomerNotificationOnOrderStatusChangedHandler(notifications, new FakeOrderRepository(order));

        await handler.HandleAsync(new OrderStatusChangedEvent(order.Id, status), CancellationToken.None);

        Assert.Equal(0, notifications.ConfirmedCalls + notifications.ReadyCalls + notifications.OutForDeliveryCalls);
    }

    [Fact]
    public async Task HandleAsync_OrderNoLongerExists_DoesNotThrow()
    {
        var notifications = new FakeNotificationService();
        var handler = new SendCustomerNotificationOnOrderStatusChangedHandler(notifications, new FakeOrderRepository(null));

        await handler.HandleAsync(new OrderStatusChangedEvent(Guid.NewGuid(), OrderStatus.Confirmed), CancellationToken.None);

        Assert.Equal(0, notifications.ConfirmedCalls);
    }
}
