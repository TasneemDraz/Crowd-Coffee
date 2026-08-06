using CrowdCoffee.Application.Dashboard;
using CrowdCoffee.Application.Inventory;
using CrowdCoffee.Domain.Inventory;

namespace CrowdCoffee.Application.Tests.Dashboard;

public class GetDailyDashboardHandlerTests
{
    private class FakeDashboardRepository(DashboardDayAggregate aggregate) : IDashboardRepository
    {
        public Task<DashboardDayAggregate> GetTodayAggregateAsync(DateTime nowUtc, CancellationToken cancellationToken) =>
            Task.FromResult(aggregate);
    }

    private class FakeStockRepository(int lowStockCount) : IStockRepository
    {
        public Task<StockItem?> GetByIdAsync(Guid stockItemId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<StockItem>> GetAllStockItemsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<StockItem>> GetStockItemsAsync(IReadOnlyCollection<Guid> stockItemIds, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<StockItem>> GetLowStockItemsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StockItem>>(
                Enumerable.Range(0, lowStockCount).Select(_ => new StockItem(Guid.NewGuid(), "Item", StockUnit.Gram, reorderPoint: 10)).ToList());

        public Task<IReadOnlyList<StockMovement>> GetMovementsForStockItemAsync(Guid stockItemId, int take, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<decimal> SumMovementsAsync(Guid stockItemId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> HasMovementForReferenceAsync(string referenceType, Guid referenceId, StockMovementType movementType, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void AddStockMovement(StockMovement movement) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    [Fact]
    public async Task ComputesAverageOrderValue_FromRevenueAndOrderCount()
    {
        var aggregate = new DashboardDayAggregate(
            new DateOnly(2026, 8, 6), Revenue: 1000m, OrderCount: 4, DeliveryCount: 3, PickupCount: 1,
            TopSellingItems: [new TopSellingItemDto("Latte", 6)]);
        var handler = new GetDailyDashboardHandler(new FakeDashboardRepository(aggregate), new FakeStockRepository(lowStockCount: 2));

        var dashboard = await handler.HandleAsync(DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(1000m, dashboard.Revenue);
        Assert.Equal(4, dashboard.OrderCount);
        Assert.Equal(250m, dashboard.AverageOrderValue);
        Assert.Equal(3, dashboard.DeliveryCount);
        Assert.Equal(1, dashboard.PickupCount);
        Assert.Equal(2, dashboard.LowStockCount);
        Assert.Equal("Latte", dashboard.TopSellingItems.Single().ProductName);
    }

    [Fact]
    public async Task ZeroOrders_AverageOrderValueIsZero_NotDivideByZero()
    {
        var aggregate = new DashboardDayAggregate(new DateOnly(2026, 8, 6), Revenue: 0m, OrderCount: 0, DeliveryCount: 0, PickupCount: 0, TopSellingItems: []);
        var handler = new GetDailyDashboardHandler(new FakeDashboardRepository(aggregate), new FakeStockRepository(lowStockCount: 0));

        var dashboard = await handler.HandleAsync(DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(0m, dashboard.AverageOrderValue);
    }
}
