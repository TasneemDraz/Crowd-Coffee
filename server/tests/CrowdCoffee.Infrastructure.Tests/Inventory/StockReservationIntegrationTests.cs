using CrowdCoffee.Domain.Inventory;
using CrowdCoffee.Domain.Ordering;
using CrowdCoffee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CrowdCoffee.Infrastructure.Tests.Inventory;

/// <summary>
/// Exercises the reservation flow against the real local SQL Server database, including the
/// flagship behavior Story 4.1 exists for: cancelling an order auto-releases its stock
/// reservation through the actual domain-event dispatch pipeline (Order.TransitionTo raises
/// OrderCancelledEvent -> CrowdCoffeeDbContext.SaveChangesAsync dispatches it ->
/// ReleaseStockOnOrderCancelledHandler calls StockReservationService) — not a shortcut that
/// calls the release logic directly.
/// </summary>
public class StockReservationIntegrationTests
{
    [Fact]
    public async Task CancellingOrder_AutoReleasesReservation_ThroughDomainEventPipeline()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var reservationService = scope.ServiceProvider.GetRequiredService<Application.Inventory.StockReservationService>();

        var productId = await dbContext.Products.Select(p => p.Id).FirstAsync();

        var stockItem = new StockItem(Guid.NewGuid(), $"Test Beans {Guid.NewGuid()}", StockUnit.Gram, reorderPoint: 200, initialQuantityOnHand: 1000);
        var recipe = Recipe.ForProduct(Guid.NewGuid(), productId, stockItem.Id, quantityPerUnit: 18);
        var order = Order.CreateCash(
            Guid.NewGuid(), $"CRW-TEST-{Guid.NewGuid():N}"[..20], "Integration Test", "0100000000", OrderMode.Pickup,
            null, null, null, subtotal: 300, deliveryFee: 0, idempotencyKey: Guid.NewGuid().ToString(), createdAtUtc: DateTime.UtcNow);
        var orderItem = new OrderItem(Guid.NewGuid(), order.Id, productId, "Test Latte", unitPrice: 100, quantity: 3);

        dbContext.StockItems.Add(stockItem);
        dbContext.Recipes.Add(recipe);
        dbContext.Orders.Add(order);
        dbContext.OrderItems.Add(orderItem);
        await dbContext.SaveChangesAsync();

        try
        {
            // Stock is only ever reserved once an order has actually reached Confirmed (§8) — an
            // order cancelled while still Pending never had anything reserved to release, and
            // Order.TransitionTo deliberately raises no OrderCancelledEvent for that case (it
            // would otherwise claw back some other order's legitimate reservation on the same
            // StockItem). Transition through Confirmed first so the cancel below is realistic.
            var confirmResult = order.TransitionTo(OrderStatus.Confirmed, "integration-test", DateTime.UtcNow);
            Assert.True(confirmResult.IsSuccess);
            await dbContext.SaveChangesAsync();

            var reserveResult = await reservationService.ReserveForOrderAsync(order.Id, CancellationToken.None);
            Assert.True(reserveResult.IsSuccess);

            var afterReserve = await dbContext.StockItems.AsNoTracking().FirstAsync(s => s.Id == stockItem.Id);
            Assert.Equal(54, afterReserve.QuantityReserved); // 3 units * 18g

            // Reload fresh (mirrors how a real request would load the order, separate from the
            // reservation call above) and drive it through the actual state machine.
            var orderToCancel = await dbContext.Orders.FirstAsync(o => o.Id == order.Id);
            var transitionResult = orderToCancel.TransitionTo(OrderStatus.Cancelled, "integration-test", DateTime.UtcNow, "test cleanup");
            Assert.True(transitionResult.IsSuccess);

            await dbContext.SaveChangesAsync(); // this is what dispatches OrderCancelledEvent

            var afterCancel = await dbContext.StockItems.AsNoTracking().FirstAsync(s => s.Id == stockItem.Id);
            Assert.Equal(0, afterCancel.QuantityReserved);
        }
        finally
        {
            await CleanupAsync(dbContext, order.Id, orderItem.Id, recipe.Id, stockItem.Id);
        }
    }

    [Fact]
    public async Task FulfillingOrder_DecrementsOnHandAndWritesSaleMovement_AndIsIdempotent()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var reservationService = scope.ServiceProvider.GetRequiredService<Application.Inventory.StockReservationService>();

        var productId = await dbContext.Products.Select(p => p.Id).FirstAsync();

        var stockItem = new StockItem(Guid.NewGuid(), $"Test Beans {Guid.NewGuid()}", StockUnit.Gram, reorderPoint: 200, initialQuantityOnHand: 1000);
        var recipe = Recipe.ForProduct(Guid.NewGuid(), productId, stockItem.Id, quantityPerUnit: 18);
        var order = Order.CreateCash(
            Guid.NewGuid(), $"CRW-TEST-{Guid.NewGuid():N}"[..20], "Integration Test", "0100000000", OrderMode.Pickup,
            null, null, null, subtotal: 100, deliveryFee: 0, idempotencyKey: Guid.NewGuid().ToString(), createdAtUtc: DateTime.UtcNow);
        var orderItem = new OrderItem(Guid.NewGuid(), order.Id, productId, "Test Latte", unitPrice: 100, quantity: 1);

        dbContext.StockItems.Add(stockItem);
        dbContext.Recipes.Add(recipe);
        dbContext.Orders.Add(order);
        dbContext.OrderItems.Add(orderItem);
        await dbContext.SaveChangesAsync();

        try
        {
            await reservationService.ReserveForOrderAsync(order.Id, CancellationToken.None);

            var fulfillResult = await reservationService.FulfillForOrderAsync(order.Id, "integration-test", DateTime.UtcNow, CancellationToken.None);
            Assert.True(fulfillResult.IsSuccess);

            var afterFulfill = await dbContext.StockItems.AsNoTracking().FirstAsync(s => s.Id == stockItem.Id);
            Assert.Equal(982, afterFulfill.QuantityOnHand); // 1000 - 18
            Assert.Equal(0, afterFulfill.QuantityReserved);

            var saleMovementCount = await dbContext.StockMovements.CountAsync(m => m.ReferenceType == "Order" && m.ReferenceId == order.Id);
            Assert.Equal(1, saleMovementCount);

            // Second fulfill attempt must be a no-op, not a second deduction.
            var secondFulfillResult = await reservationService.FulfillForOrderAsync(order.Id, "integration-test", DateTime.UtcNow, CancellationToken.None);
            Assert.True(secondFulfillResult.IsSuccess);

            var afterSecondFulfill = await dbContext.StockItems.AsNoTracking().FirstAsync(s => s.Id == stockItem.Id);
            Assert.Equal(982, afterSecondFulfill.QuantityOnHand);

            var saleMovementCountAfterSecond = await dbContext.StockMovements.CountAsync(m => m.ReferenceType == "Order" && m.ReferenceId == order.Id);
            Assert.Equal(1, saleMovementCountAfterSecond);
        }
        finally
        {
            await CleanupAsync(dbContext, order.Id, orderItem.Id, recipe.Id, stockItem.Id);
        }
    }

    [Fact]
    public async Task CompletingOrder_AutoFulfillsReservation_ThroughDomainEventPipeline()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var reservationService = scope.ServiceProvider.GetRequiredService<Application.Inventory.StockReservationService>();

        var productId = await dbContext.Products.Select(p => p.Id).FirstAsync();

        var stockItem = new StockItem(Guid.NewGuid(), $"Test Beans {Guid.NewGuid()}", StockUnit.Gram, reorderPoint: 200, initialQuantityOnHand: 1000);
        var recipe = Recipe.ForProduct(Guid.NewGuid(), productId, stockItem.Id, quantityPerUnit: 18);
        var order = Order.CreateCash(
            Guid.NewGuid(), $"CRW-TEST-{Guid.NewGuid():N}"[..20], "Integration Test", "0100000000", OrderMode.Pickup,
            null, null, null, subtotal: 100, deliveryFee: 0, idempotencyKey: Guid.NewGuid().ToString(), createdAtUtc: DateTime.UtcNow);
        var orderItem = new OrderItem(Guid.NewGuid(), order.Id, productId, "Test Latte", unitPrice: 100, quantity: 1);

        dbContext.StockItems.Add(stockItem);
        dbContext.Recipes.Add(recipe);
        dbContext.Orders.Add(order);
        dbContext.OrderItems.Add(orderItem);
        await dbContext.SaveChangesAsync();

        try
        {
            await reservationService.ReserveForOrderAsync(order.Id, CancellationToken.None);

            // Drive the order through the real state machine to PickedUp — the completion
            // transition itself (not a direct FulfillForOrderAsync call) is what should trigger
            // fulfillment, via OrderCompletedEvent -> FulfillStockOnOrderCompletedHandler.
            foreach (var next in new[] { OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Ready, OrderStatus.PickedUp })
            {
                var order2 = await dbContext.Orders.FirstAsync(o => o.Id == order.Id);
                var transitionResult = order2.TransitionTo(next, "integration-test", DateTime.UtcNow);
                Assert.True(transitionResult.IsSuccess);
                await dbContext.SaveChangesAsync();
            }

            var afterCompletion = await dbContext.StockItems.AsNoTracking().FirstAsync(s => s.Id == stockItem.Id);
            Assert.Equal(982, afterCompletion.QuantityOnHand); // 1000 - 18
            Assert.Equal(0, afterCompletion.QuantityReserved);

            var saleMovementCount = await dbContext.StockMovements.CountAsync(m => m.ReferenceType == "Order" && m.ReferenceId == order.Id);
            Assert.Equal(1, saleMovementCount);
        }
        finally
        {
            await CleanupAsync(dbContext, order.Id, orderItem.Id, recipe.Id, stockItem.Id);
        }
    }

    private static async Task CleanupAsync(CrowdCoffeeDbContext dbContext, Guid orderId, Guid orderItemId, Guid recipeId, Guid stockItemId)
    {
        await dbContext.StockMovements.Where(m => m.StockItemId == stockItemId).ExecuteDeleteAsync();
        await dbContext.OrderStatusHistories.Where(h => h.OrderId == orderId).ExecuteDeleteAsync();
        await dbContext.OrderItems.Where(i => i.Id == orderItemId).ExecuteDeleteAsync();
        await dbContext.Orders.Where(o => o.Id == orderId).ExecuteDeleteAsync();
        await dbContext.Recipes.Where(r => r.Id == recipeId).ExecuteDeleteAsync();
        await dbContext.StockItems.Where(s => s.Id == stockItemId).ExecuteDeleteAsync();
    }
}
