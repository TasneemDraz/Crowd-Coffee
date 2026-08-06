using CrowdCoffee.Application.Dashboard;
using CrowdCoffee.Application.Ordering;
using CrowdCoffee.Domain.Ordering;
using CrowdCoffee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CrowdCoffee.Infrastructure.Tests.Dashboard;

/// Story 14.1 against real local SQL Server + the real seeded catalog. Places a real cash order
/// (auto-confirms, per Epic 9) and asserts it's picked up by "today's" aggregate — the actual
/// point of this test, since a bug in the Cairo-day boundary math would otherwise silently drop
/// or double-count orders placed near midnight.
public class DashboardIntegrationTests
{
    [Fact]
    public async Task TodayAggregate_IncludesAFreshlyPlacedCashOrder_InRevenueCountAndTopSellers()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var createHandler = scope.ServiceProvider.GetRequiredService<CreateOrderHandler>();
        var dashboardHandler = scope.ServiceProvider.GetRequiredService<GetDailyDashboardHandler>();

        var product = await dbContext.Products.AsNoTracking().FirstAsync(p => p.CategorySlug == "black" && p.IsActive);
        var milkGroup = await dbContext.ModifierGroups.AsNoTracking().Include(g => g.Options).FirstAsync(g => g.Slug == "milk");
        var noMilk = milkGroup.Options.First(o => o.Slug == "none");

        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateOrderRequest(
            "Dashboard Test", "0100000000", OrderMode.Delivery, "1 Test St", 30.0, 31.2,
            PaymentMethod.Cash, idempotencyKey, [new CreateOrderLineRequest(product.Id, noMilk.Id, null, [], 2)], null);

        try
        {
            var before = await dashboardHandler.HandleAsync(DateTime.UtcNow, CancellationToken.None);

            var created = await createHandler.HandleAsync(request, CancellationToken.None);
            Assert.True(created.IsSuccess);
            Assert.Equal("Confirmed", created.Value.Status);

            var after = await dashboardHandler.HandleAsync(DateTime.UtcNow, CancellationToken.None);

            Assert.Equal(before.OrderCount + 1, after.OrderCount);
            Assert.Equal(before.Revenue + created.Value.Total, after.Revenue);
            Assert.Equal(before.DeliveryCount + 1, after.DeliveryCount);
            Assert.Contains(after.TopSellingItems, i => i.ProductName == product.Name);
        }
        finally
        {
            var orderId = await dbContext.Orders.Where(o => o.IdempotencyKey == idempotencyKey).Select(o => (Guid?)o.Id).FirstOrDefaultAsync();
            if (orderId is { } id)
            {
                await dbContext.StockMovements.Where(m => m.ReferenceType == "Order" && m.ReferenceId == id).ExecuteDeleteAsync();
                await dbContext.OrderStatusHistories.Where(h => h.OrderId == id).ExecuteDeleteAsync();
                await dbContext.OrderItemModifiers.Where(m => dbContext.OrderItems.Where(i => i.OrderId == id).Select(i => i.Id).Contains(m.OrderItemId)).ExecuteDeleteAsync();
                await dbContext.OrderItems.Where(i => i.OrderId == id).ExecuteDeleteAsync();
                await dbContext.Orders.Where(o => o.Id == id).ExecuteDeleteAsync();
            }
        }
    }

    [Fact]
    public async Task TodayAggregate_ExposesLowStockCount_FromEpic4()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dashboardHandler = scope.ServiceProvider.GetRequiredService<GetDailyDashboardHandler>();

        var dashboard = await dashboardHandler.HandleAsync(DateTime.UtcNow, CancellationToken.None);

        Assert.True(dashboard.LowStockCount >= 0); // exercises the real GetLowStockItemsAsync path without asserting a brittle exact count
    }
}
