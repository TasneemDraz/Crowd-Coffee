using CrowdCoffee.Application.Inventory;

namespace CrowdCoffee.Application.Dashboard;

/// Story 14.1 (metrics) + Story 14.2 (LowStockCount lets the dashboard show a badge without a
/// second round trip — the actual low-stock list/reconciliation screens are separate links, not
/// duplicated here).
public class GetDailyDashboardHandler(IDashboardRepository dashboardRepository, IStockRepository stockRepository)
{
    public async Task<DailyDashboardDto> HandleAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var aggregate = await dashboardRepository.GetTodayAggregateAsync(nowUtc, cancellationToken);
        var lowStockItems = await stockRepository.GetLowStockItemsAsync(cancellationToken);

        var averageOrderValue = aggregate.OrderCount > 0
            ? Math.Round(aggregate.Revenue / aggregate.OrderCount, 2)
            : 0m;

        return new DailyDashboardDto(
            aggregate.Date, aggregate.Revenue, aggregate.OrderCount, averageOrderValue,
            aggregate.DeliveryCount, aggregate.PickupCount, aggregate.TopSellingItems, lowStockItems.Count);
    }
}
