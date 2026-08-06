using CrowdCoffee.Application.Dashboard;
using CrowdCoffee.Domain.Ordering;
using Microsoft.EntityFrameworkCore;

namespace CrowdCoffee.Infrastructure.Persistence.Repositories;

/// <summary>
/// Read-optimized aggregation straight from EF's LINQ translation (Sum/Count/GroupBy pushed down
/// to SQL) — never materializes full Order/OrderItem aggregates just to count them, per §12's
/// "read-optimized, not full aggregate loads" note on this story.
/// </summary>
public class DashboardRepository(CrowdCoffeeDbContext dbContext) : IDashboardRepository
{
    /// "Revenue" excludes orders never confirmed (Pending/PendingPayment — nothing actually sold
    /// yet) and cancelled orders (§8: a cancelled order was never fulfilled).
    private static readonly OrderStatus[] RevenueStatuses =
    [
        OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Ready,
        OrderStatus.OutForDelivery, OrderStatus.Delivered, OrderStatus.PickedUp
    ];

    public async Task<DashboardDayAggregate> GetTodayAggregateAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var today = CairoClock.Today(nowUtc);
        var (dayStartUtc, dayEndUtc) = CairoClock.DayRangeUtc(today);

        var ordersToday = dbContext.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= dayStartUtc && o.CreatedAt < dayEndUtc && RevenueStatuses.Contains(o.Status));

        var revenue = await ordersToday.SumAsync(o => (decimal?)o.Total, cancellationToken) ?? 0m;
        var orderCount = await ordersToday.CountAsync(cancellationToken);
        var deliveryCount = await ordersToday.CountAsync(o => o.Mode == OrderMode.Delivery, cancellationToken);
        var pickupCount = await ordersToday.CountAsync(o => o.Mode == OrderMode.Pickup, cancellationToken);

        // Materialized first — EF Core can't translate GroupBy over an OrderItems query filtered
        // by a correlated Orders subquery's projected ids in one shot (a nested-IQueryable-inside-
        // GroupBy shape it doesn't support); a plain in-memory id list sidesteps that entirely.
        var orderIdsToday = await ordersToday.Select(o => o.Id).ToListAsync(cancellationToken);

        // Projects to an anonymous type first, then maps to TopSellingItemDto client-side — EF
        // Core 8 doesn't reliably translate a GroupBy().Select(new SomeRecord(...)) shape (it
        // tried to re-query the grouping via AsQueryable().Sum(...) instead of pushing SUM down
        // to SQL), but the same aggregation into an anonymous type translates cleanly.
        var grouped = await dbContext.OrderItems
            .AsNoTracking()
            .Where(i => orderIdsToday.Contains(i.OrderId))
            .GroupBy(i => i.ProductName)
            .Select(g => new { ProductName = g.Key, QuantitySold = g.Sum(i => i.Quantity) })
            .OrderByDescending(g => g.QuantitySold)
            .Take(5)
            .ToListAsync(cancellationToken);

        var topSellingItems = grouped.Select(g => new TopSellingItemDto(g.ProductName, g.QuantitySold)).ToList();

        return new DashboardDayAggregate(today, revenue, orderCount, deliveryCount, pickupCount, topSellingItems);
    }
}
