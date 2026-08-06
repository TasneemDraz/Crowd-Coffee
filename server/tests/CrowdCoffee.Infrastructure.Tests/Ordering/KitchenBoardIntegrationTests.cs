using CrowdCoffee.Application.Kitchen;
using CrowdCoffee.Application.Ordering;
using CrowdCoffee.Domain.Inventory;
using CrowdCoffee.Domain.Ordering;
using CrowdCoffee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CrowdCoffee.Infrastructure.Tests.Ordering;

/// <summary>
/// Epic 12: the kitchen board query (which orders show up, and in what shape) and the one-tap
/// advance handler, against real local SQL Server + the real seeded catalog — same pattern as
/// CreateOrderIntegrationTests. The SignalR broadcast itself (BroadcastOrderStatusChangedHandler)
/// lives in Api and isn't reachable from this project; it was verified live instead (see
/// docs/EPIC_BREAKDOWN.md's Story 12.1 delivery note).
/// </summary>
public class KitchenBoardIntegrationTests
{
    [Fact]
    public async Task ConfirmedOrder_AppearsOnBoard_WithModifiersSpelledOut()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var createHandler = scope.ServiceProvider.GetRequiredService<CreateOrderHandler>();
        var boardHandler = scope.ServiceProvider.GetRequiredService<GetKitchenBoardOrdersHandler>();

        var product = await dbContext.Products.AsNoTracking().Where(p => p.CategorySlug == "classic" && p.IsActive).OrderBy(p => p.Id).Skip(3).FirstAsync();
        var milkGroup = await dbContext.ModifierGroups.AsNoTracking().Include(g => g.Options).FirstAsync(g => g.Slug == "milk");
        var oatMilk = milkGroup.Options.First(o => o.Slug == "oat");

        var stockItem = new StockItem(Guid.NewGuid(), $"Test Beans {Guid.NewGuid()}", StockUnit.Gram, reorderPoint: 200, initialQuantityOnHand: 1000);
        var recipe = Recipe.ForProduct(Guid.NewGuid(), product.Id, stockItem.Id, quantityPerUnit: 18);
        dbContext.StockItems.Add(stockItem);
        dbContext.Recipes.Add(recipe);
        await dbContext.SaveChangesAsync();

        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateOrderRequest(
            "Kitchen Test", "0100000000", OrderMode.Pickup, null, null, null,
            PaymentMethod.Cash, idempotencyKey,
            [new CreateOrderLineRequest(product.Id, oatMilk.Id, null, [], 1)], null);

        try
        {
            var created = await createHandler.HandleAsync(request, CancellationToken.None);
            Assert.True(created.IsSuccess);

            var board = await boardHandler.HandleAsync(CancellationToken.None);
            var boardOrder = Assert.Single(board.Where(o => o.Id == created.Value.Id));

            Assert.Equal("Confirmed", boardOrder.Status);
            var line = Assert.Single(boardOrder.Lines);
            Assert.Equal("Oat Milk", Assert.Single(line.Modifiers));
        }
        finally
        {
            await CleanupAsync(dbContext, idempotencyKey, recipe.Id, stockItem.Id);
        }
    }

    [Fact]
    public async Task PendingPaymentOrder_DoesNotAppearOnBoard()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var createHandler = scope.ServiceProvider.GetRequiredService<CreateOrderHandler>();
        var boardHandler = scope.ServiceProvider.GetRequiredService<GetKitchenBoardOrdersHandler>();

        var product = await dbContext.Products.AsNoTracking().FirstAsync(p => p.CategorySlug == "black" && p.IsActive);
        var milkGroup = await dbContext.ModifierGroups.AsNoTracking().Include(g => g.Options).FirstAsync(g => g.Slug == "milk");
        var noMilk = milkGroup.Options.First(o => o.Slug == "none");

        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateOrderRequest(
            "Kitchen Test", "0100000000", OrderMode.Delivery, "1 Test St", 30.0, 31.2,
            PaymentMethod.Card, idempotencyKey,
            [new CreateOrderLineRequest(product.Id, noMilk.Id, null, [], 1)], null);

        try
        {
            var created = await createHandler.HandleAsync(request, CancellationToken.None);
            Assert.True(created.IsSuccess);
            Assert.Equal("PendingPayment", created.Value.Status);

            var board = await boardHandler.HandleAsync(CancellationToken.None);
            Assert.DoesNotContain(board, o => o.Id == created.Value.Id);
        }
        finally
        {
            await CleanupAsync(dbContext, idempotencyKey, null, null);
        }
    }

    [Fact]
    public async Task Advance_WalksConfirmedThroughPickedUp_AndLeavesTheBoardOnceDone()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var createHandler = scope.ServiceProvider.GetRequiredService<CreateOrderHandler>();
        var advanceHandler = scope.ServiceProvider.GetRequiredService<AdvanceOrderStatusHandler>();
        var boardHandler = scope.ServiceProvider.GetRequiredService<GetKitchenBoardOrdersHandler>();

        var product = await dbContext.Products.AsNoTracking().Where(p => p.CategorySlug == "classic" && p.IsActive).OrderBy(p => p.Id).Skip(4).FirstAsync();
        var milkGroup = await dbContext.ModifierGroups.AsNoTracking().Include(g => g.Options).FirstAsync(g => g.Slug == "milk");
        var oatMilk = milkGroup.Options.First(o => o.Slug == "oat");

        var stockItem = new StockItem(Guid.NewGuid(), $"Test Beans {Guid.NewGuid()}", StockUnit.Gram, reorderPoint: 200, initialQuantityOnHand: 1000);
        var recipe = Recipe.ForProduct(Guid.NewGuid(), product.Id, stockItem.Id, quantityPerUnit: 18);
        dbContext.StockItems.Add(stockItem);
        dbContext.Recipes.Add(recipe);
        await dbContext.SaveChangesAsync();

        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateOrderRequest(
            "Kitchen Test", "0100000000", OrderMode.Pickup, null, null, null,
            PaymentMethod.Cash, idempotencyKey,
            [new CreateOrderLineRequest(product.Id, oatMilk.Id, null, [], 1)], null);

        try
        {
            var created = await createHandler.HandleAsync(request, CancellationToken.None);
            Assert.True(created.IsSuccess);
            var orderId = created.Value.Id;

            var toPreparing = await advanceHandler.HandleAsync(orderId, "barista1", DateTime.UtcNow, CancellationToken.None);
            Assert.True(toPreparing.IsSuccess);
            Assert.Equal("Preparing", toPreparing.Value.Status);

            var toReady = await advanceHandler.HandleAsync(orderId, "barista1", DateTime.UtcNow, CancellationToken.None);
            Assert.True(toReady.IsSuccess);
            Assert.Equal("Ready", toReady.Value.Status);

            var boardWhileReady = await boardHandler.HandleAsync(CancellationToken.None);
            Assert.Contains(boardWhileReady, o => o.Id == orderId);

            var toPickedUp = await advanceHandler.HandleAsync(orderId, "barista1", DateTime.UtcNow, CancellationToken.None);
            Assert.True(toPickedUp.IsSuccess);
            Assert.Equal("PickedUp", toPickedUp.Value.Status); // Pickup order -> PickedUp, not OutForDelivery

            var boardAfterPickup = await boardHandler.HandleAsync(CancellationToken.None);
            Assert.DoesNotContain(boardAfterPickup, o => o.Id == orderId);

            var noNextStep = await advanceHandler.HandleAsync(orderId, "barista1", DateTime.UtcNow, CancellationToken.None);
            Assert.True(noNextStep.IsFailure);
            Assert.Equal("Order.NoNextKitchenStatus", noNextStep.Error!.Code);
        }
        finally
        {
            await CleanupAsync(dbContext, idempotencyKey, recipe.Id, stockItem.Id);
        }
    }

    private static async Task CleanupAsync(CrowdCoffeeDbContext dbContext, string idempotencyKey, Guid? recipeId, Guid? stockItemId)
    {
        var orderId = await dbContext.Orders.Where(o => o.IdempotencyKey == idempotencyKey).Select(o => (Guid?)o.Id).FirstOrDefaultAsync();
        if (orderId is { } id)
        {
            if (stockItemId is { } sid)
            {
                await dbContext.StockMovements.Where(m => m.StockItemId == sid).ExecuteDeleteAsync();
            }

            await dbContext.OrderStatusHistories.Where(h => h.OrderId == id).ExecuteDeleteAsync();
            await dbContext.OrderItemModifiers.Where(m => dbContext.OrderItems.Where(i => i.OrderId == id).Select(i => i.Id).Contains(m.OrderItemId)).ExecuteDeleteAsync();
            await dbContext.OrderItems.Where(i => i.OrderId == id).ExecuteDeleteAsync();
            await dbContext.Orders.Where(o => o.Id == id).ExecuteDeleteAsync();
        }

        if (recipeId is { } rid)
        {
            await dbContext.Recipes.Where(r => r.Id == rid).ExecuteDeleteAsync();
        }

        if (stockItemId is { } sid2)
        {
            await dbContext.StockItems.Where(s => s.Id == sid2).ExecuteDeleteAsync();
        }
    }
}
