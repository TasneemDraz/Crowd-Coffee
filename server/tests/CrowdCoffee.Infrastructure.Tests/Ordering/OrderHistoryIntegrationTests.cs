using CrowdCoffee.Application.Ordering;
using CrowdCoffee.Domain.Ordering;
using CrowdCoffee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CrowdCoffee.Infrastructure.Tests.Ordering;

/// Story 13.4 against real local SQL Server + the real seeded catalog.
public class OrderHistoryIntegrationTests
{
    [Fact]
    public async Task Search_FiltersByStatusModeAndPaymentMethod()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var createHandler = scope.ServiceProvider.GetRequiredService<CreateOrderHandler>();
        var searchHandler = scope.ServiceProvider.GetRequiredService<SearchOrdersHandler>();

        var product = await dbContext.Products.AsNoTracking().FirstAsync(p => p.CategorySlug == "black" && p.IsActive);
        var milkGroup = await dbContext.ModifierGroups.AsNoTracking().Include(g => g.Options).FirstAsync(g => g.Slug == "milk");
        var noMilk = milkGroup.Options.First(o => o.Slug == "none");

        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateOrderRequest(
            "History Test", "0100000000", OrderMode.Pickup, null, null, null,
            PaymentMethod.Cash, idempotencyKey, [new CreateOrderLineRequest(product.Id, noMilk.Id, null, [], 1)], null);

        try
        {
            var created = await createHandler.HandleAsync(request, CancellationToken.None);
            Assert.True(created.IsSuccess);

            var matching = await searchHandler.HandleAsync(
                new OrderSearchFilters(null, null, OrderStatus.Confirmed, OrderMode.Pickup, PaymentMethod.Cash), CancellationToken.None);
            Assert.Contains(matching, o => o.Id == created.Value.Id);

            var wrongStatus = await searchHandler.HandleAsync(
                new OrderSearchFilters(null, null, OrderStatus.Cancelled, null, null), CancellationToken.None);
            Assert.DoesNotContain(wrongStatus, o => o.Id == created.Value.Id);

            var wrongPaymentMethod = await searchHandler.HandleAsync(
                new OrderSearchFilters(null, null, null, null, PaymentMethod.Card), CancellationToken.None);
            Assert.DoesNotContain(wrongPaymentMethod, o => o.Id == created.Value.Id);
        }
        finally
        {
            await CleanupAsync(dbContext, idempotencyKey);
        }
    }

    [Fact]
    public async Task GetDetail_ReturnsFullStatusHistory_AfterMultipleTransitions()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var createHandler = scope.ServiceProvider.GetRequiredService<CreateOrderHandler>();
        var advanceHandler = scope.ServiceProvider.GetRequiredService<AdvanceOrderStatusHandler>();
        var detailHandler = scope.ServiceProvider.GetRequiredService<GetOrderDetailHandler>();

        var product = await dbContext.Products.AsNoTracking().FirstAsync(p => p.CategorySlug == "black" && p.IsActive);
        var milkGroup = await dbContext.ModifierGroups.AsNoTracking().Include(g => g.Options).FirstAsync(g => g.Slug == "milk");
        var noMilk = milkGroup.Options.First(o => o.Slug == "none");

        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateOrderRequest(
            "History Test", "0100000000", OrderMode.Pickup, null, null, null,
            PaymentMethod.Cash, idempotencyKey, [new CreateOrderLineRequest(product.Id, noMilk.Id, null, [], 1)], null);

        try
        {
            var created = await createHandler.HandleAsync(request, CancellationToken.None);
            Assert.True(created.IsSuccess);

            await advanceHandler.HandleAsync(created.Value.Id, "barista1", DateTime.UtcNow, CancellationToken.None); // -> Preparing

            var detail = await detailHandler.HandleAsync(created.Value.Id, CancellationToken.None);

            Assert.True(detail.IsSuccess);
            Assert.Equal("Preparing", detail.Value.Status);
            Assert.Equal(2, detail.Value.StatusHistory.Count); // Pending->Confirmed, Confirmed->Preparing
            Assert.Equal("Pending", detail.Value.StatusHistory[0].FromStatus);
            Assert.Equal("Confirmed", detail.Value.StatusHistory[0].ToStatus);
            Assert.Equal("Confirmed", detail.Value.StatusHistory[1].FromStatus);
            Assert.Equal("Preparing", detail.Value.StatusHistory[1].ToStatus);
            Assert.Equal(noMilk.Name, Assert.Single(detail.Value.Lines).Modifiers.Single());
        }
        finally
        {
            await CleanupAsync(dbContext, idempotencyKey);
        }
    }

    [Fact]
    public async Task GetDetail_UnknownOrder_ReturnsNotFound()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var detailHandler = scope.ServiceProvider.GetRequiredService<GetOrderDetailHandler>();

        var result = await detailHandler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Order.NotFound", result.Error!.Code);
    }

    private static async Task CleanupAsync(CrowdCoffeeDbContext dbContext, string idempotencyKey)
    {
        var orderId = await dbContext.Orders.Where(o => o.IdempotencyKey == idempotencyKey).Select(o => (Guid?)o.Id).FirstOrDefaultAsync();
        if (orderId is not { } id)
        {
            return;
        }

        await dbContext.OrderStatusHistories.Where(h => h.OrderId == id).ExecuteDeleteAsync();
        await dbContext.OrderItemModifiers.Where(m => dbContext.OrderItems.Where(i => i.OrderId == id).Select(i => i.Id).Contains(m.OrderItemId)).ExecuteDeleteAsync();
        await dbContext.OrderItems.Where(i => i.OrderId == id).ExecuteDeleteAsync();
        await dbContext.Orders.Where(o => o.Id == id).ExecuteDeleteAsync();
    }
}
