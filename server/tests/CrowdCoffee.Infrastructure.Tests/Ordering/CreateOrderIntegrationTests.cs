using CrowdCoffee.Application.Ordering;
using CrowdCoffee.Domain.Inventory;
using CrowdCoffee.Domain.Ordering;
using CrowdCoffee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CrowdCoffee.Infrastructure.Tests.Ordering;

/// <summary>
/// Exercises CreateOrderHandler against real local SQL Server + the real seeded catalog. Story
/// 9.2's AC is "single transaction: validate → reserve → persist; partial failure leaves no
/// orphan reservation" — the insufficient-stock test below is the one that actually proves that,
/// by asserting nothing at all persisted (no Order row, no reservation) when reservation fails
/// partway through the handler.
/// </summary>
public class CreateOrderIntegrationTests
{
    [Fact]
    public async Task CashOrder_WithSufficientStock_AutoConfirmsAndReservesStock()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<CreateOrderHandler>();

        // Distinct product per test (Skip(0) here; sibling tests use different offsets) — every
        // test in this file that attaches its own Recipe to a "classic" product must use a
        // DIFFERENT product than its siblings. GetRequiredQuantitiesAsync aggregates *every*
        // Recipe row matching a product id, so two tests sharing one product would see each
        // other's recipes (and, worse, each other's stock levels) when run in parallel — xUnit
        // runs test classes concurrently by default.
        var product = await dbContext.Products.AsNoTracking().Where(p => p.CategorySlug == "classic" && p.IsActive).OrderBy(p => p.Id).Skip(0).FirstAsync();
        var milkGroup = await dbContext.ModifierGroups.AsNoTracking().Include(g => g.Options).FirstAsync(g => g.Slug == "milk");
        var oatMilk = milkGroup.Options.First(o => o.Slug == "oat");

        var stockItem = new StockItem(Guid.NewGuid(), $"Test Beans {Guid.NewGuid()}", StockUnit.Gram, reorderPoint: 200, initialQuantityOnHand: 1000);
        var recipe = Recipe.ForProduct(Guid.NewGuid(), product.Id, stockItem.Id, quantityPerUnit: 18);
        dbContext.StockItems.Add(stockItem);
        dbContext.Recipes.Add(recipe);
        await dbContext.SaveChangesAsync();

        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateOrderRequest(
            "Integration Test", "0100000000", OrderMode.Pickup, null, null, null,
            PaymentMethod.Cash, idempotencyKey,
            [new CreateOrderLineRequest(product.Id, oatMilk.Id, null, [], 2)], null);

        try
        {
            var result = await handler.HandleAsync(request, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("Confirmed", result.Value.Status);
            Assert.Equal("Cash", result.Value.PaymentMethod);
            Assert.Matches(@"^CRW-\d{6}-\d{4}$", result.Value.OrderNumber);
            Assert.Equal(product.Price + oatMilk.PriceDelta, result.Value.Lines.Single().UnitPrice);
            Assert.Equal(0, result.Value.DeliveryFee); // pickup

            var afterReserve = await dbContext.StockItems.AsNoTracking().FirstAsync(s => s.Id == stockItem.Id);
            Assert.Equal(36, afterReserve.QuantityReserved); // 2 units * 18g

            var persisted = await dbContext.Orders.AsNoTracking().FirstAsync(o => o.IdempotencyKey == idempotencyKey);
            Assert.Equal(OrderStatus.Confirmed, persisted.Status);
        }
        finally
        {
            await CleanupAsync(dbContext, idempotencyKey, recipe.Id, stockItem.Id);
        }
    }

    [Fact]
    public async Task CashOrder_WithInsufficientStock_RollsBackEntirely_NoOrphanOrderOrReservation()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<CreateOrderHandler>();

        var product = await dbContext.Products.AsNoTracking().Where(p => p.CategorySlug == "classic" && p.IsActive).OrderBy(p => p.Id).Skip(1).FirstAsync();
        var milkGroup = await dbContext.ModifierGroups.AsNoTracking().Include(g => g.Options).FirstAsync(g => g.Slug == "milk");
        var oatMilk = milkGroup.Options.First(o => o.Slug == "oat");

        // Only 10g on hand; the order needs 18g. Insufficient by construction.
        var stockItem = new StockItem(Guid.NewGuid(), $"Test Beans {Guid.NewGuid()}", StockUnit.Gram, reorderPoint: 200, initialQuantityOnHand: 10);
        var recipe = Recipe.ForProduct(Guid.NewGuid(), product.Id, stockItem.Id, quantityPerUnit: 18);
        dbContext.StockItems.Add(stockItem);
        dbContext.Recipes.Add(recipe);
        await dbContext.SaveChangesAsync();

        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateOrderRequest(
            "Integration Test", "0100000000", OrderMode.Pickup, null, null, null,
            PaymentMethod.Cash, idempotencyKey,
            [new CreateOrderLineRequest(product.Id, oatMilk.Id, null, [], 1)], null);

        try
        {
            var result = await handler.HandleAsync(request, CancellationToken.None);

            Assert.True(result.IsFailure);
            Assert.Equal("Stock.InsufficientStock", result.Error!.Code);

            var orderExists = await dbContext.Orders.AsNoTracking().AnyAsync(o => o.IdempotencyKey == idempotencyKey);
            Assert.False(orderExists, "A failed order creation must not leave an order row behind.");

            var afterFailure = await dbContext.StockItems.AsNoTracking().FirstAsync(s => s.Id == stockItem.Id);
            Assert.Equal(0, afterFailure.QuantityReserved);
            Assert.Equal(10, afterFailure.QuantityOnHand);
        }
        finally
        {
            await CleanupAsync(dbContext, idempotencyKey, recipe.Id, stockItem.Id);
        }
    }

    [Fact]
    public async Task CardOrder_StaysPendingPayment_NoStockReserved()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<CreateOrderHandler>();

        var product = await dbContext.Products.AsNoTracking().Where(p => p.CategorySlug == "classic" && p.IsActive).OrderBy(p => p.Id).Skip(2).FirstAsync();
        var milkGroup = await dbContext.ModifierGroups.AsNoTracking().Include(g => g.Options).FirstAsync(g => g.Slug == "milk");
        var oatMilk = milkGroup.Options.First(o => o.Slug == "oat");

        var stockItem = new StockItem(Guid.NewGuid(), $"Test Beans {Guid.NewGuid()}", StockUnit.Gram, reorderPoint: 200, initialQuantityOnHand: 1000);
        var recipe = Recipe.ForProduct(Guid.NewGuid(), product.Id, stockItem.Id, quantityPerUnit: 18);
        dbContext.StockItems.Add(stockItem);
        dbContext.Recipes.Add(recipe);
        await dbContext.SaveChangesAsync();

        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateOrderRequest(
            "Integration Test", "0100000000", OrderMode.Delivery, "12 Corniche St", 31.25, 32.30,
            PaymentMethod.Card, idempotencyKey,
            [new CreateOrderLineRequest(product.Id, oatMilk.Id, null, [], 1)], null);

        try
        {
            var result = await handler.HandleAsync(request, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("PendingPayment", result.Value.Status);
            Assert.Equal("Card", result.Value.PaymentMethod);
            Assert.Equal(30, result.Value.DeliveryFee); // delivery, flat fee

            var afterCreate = await dbContext.StockItems.AsNoTracking().FirstAsync(s => s.Id == stockItem.Id);
            Assert.Equal(0, afterCreate.QuantityReserved); // §8: never reserve at PendingPayment
        }
        finally
        {
            await CleanupAsync(dbContext, idempotencyKey, recipe.Id, stockItem.Id);
        }
    }

    [Fact]
    public async Task RepeatIdempotencyKey_ReturnsOriginalOrder_DoesNotDuplicate()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<CreateOrderHandler>();

        var product = await dbContext.Products.AsNoTracking().FirstAsync(p => p.CategorySlug == "black" && p.IsActive);
        var milkGroup = await dbContext.ModifierGroups.AsNoTracking().Include(g => g.Options).FirstAsync(g => g.Slug == "milk");
        var noMilk = milkGroup.Options.First(o => o.Slug == "none");

        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateOrderRequest(
            "Integration Test", "0100000000", OrderMode.Pickup, null, null, null,
            PaymentMethod.Cash, idempotencyKey,
            [new CreateOrderLineRequest(product.Id, noMilk.Id, null, [], 1)], null);

        try
        {
            var first = await handler.HandleAsync(request, CancellationToken.None);
            var second = await handler.HandleAsync(request, CancellationToken.None);

            Assert.True(first.IsSuccess);
            Assert.True(second.IsSuccess);
            Assert.Equal(first.Value.Id, second.Value.Id);
            Assert.Equal(first.Value.OrderNumber, second.Value.OrderNumber);

            var count = await dbContext.Orders.CountAsync(o => o.IdempotencyKey == idempotencyKey);
            Assert.Equal(1, count);
        }
        finally
        {
            await CleanupAsync(dbContext, idempotencyKey, null, null);
        }
    }

    [Fact]
    public async Task NonDrinkLine_WithNoModifiers_PricesAtBase()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<CreateOrderHandler>();

        var drinkCategorySlugs = await dbContext.ProductModifierGroups.Select(pmg => pmg.CategorySlug).Distinct().ToListAsync();
        var nonDrinkProduct = await dbContext.Products.AsNoTracking()
            .FirstAsync(p => p.IsActive && !drinkCategorySlugs.Contains(p.CategorySlug));

        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateOrderRequest(
            "Integration Test", "0100000000", OrderMode.Pickup, null, null, null,
            PaymentMethod.Cash, idempotencyKey,
            [new CreateOrderLineRequest(nonDrinkProduct.Id, null, null, [], 1)], null);

        try
        {
            var result = await handler.HandleAsync(request, CancellationToken.None);

            Assert.True(result.IsSuccess);
            var line = Assert.Single(result.Value.Lines);
            Assert.Equal(nonDrinkProduct.Price, line.UnitPrice);
            Assert.Equal(nonDrinkProduct.Name, line.Name);
        }
        finally
        {
            await CleanupAsync(dbContext, idempotencyKey, null, null);
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
