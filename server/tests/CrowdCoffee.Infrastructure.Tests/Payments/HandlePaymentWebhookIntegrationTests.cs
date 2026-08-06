using System.Security.Cryptography;
using System.Text;
using CrowdCoffee.Application.Inventory;
using CrowdCoffee.Application.Ordering;
using CrowdCoffee.Application.Payments;
using CrowdCoffee.Domain.Inventory;
using CrowdCoffee.Domain.Ordering;
using CrowdCoffee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CrowdCoffee.Infrastructure.Tests.Payments;

/// <summary>
/// Story 10.3 end-to-end against real local SQL Server: a card order created at PendingPayment
/// (via the real CreateOrderHandler, matching how Story 9.2 actually produces one) reaches
/// Confirmed + reserves stock only once a correctly-HMAC-signed "successful" webhook arrives —
/// not at order creation, per §8's "never reserve at PendingPayment" rule this project builds
/// around throughout Epic 9/10.
/// </summary>
public class HandlePaymentWebhookIntegrationTests
{
    private const string HmacSecret = "test-hmac-secret";

    [Fact]
    public async Task SuccessfulWebhook_ConfirmsOrderAndReservesStock()
    {
        await using var provider = TestServiceProviderFactory.Build(HmacSecret);
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var createOrderHandler = scope.ServiceProvider.GetRequiredService<CreateOrderHandler>();
        var webhookHandler = scope.ServiceProvider.GetRequiredService<HandlePaymentWebhookHandler>();

        // Skip(3): distinct from the products CreateOrderIntegrationTests' three "classic" tests
        // use (Skip(0..2)) — see the comment there on why sibling tests must not share a product
        // when each attaches its own Recipe.
        var product = await dbContext.Products.AsNoTracking().Where(p => p.CategorySlug == "classic" && p.IsActive).OrderBy(p => p.Id).Skip(3).FirstAsync();
        var milkGroup = await dbContext.ModifierGroups.AsNoTracking().Include(g => g.Options).FirstAsync(g => g.Slug == "milk");
        var oatMilk = milkGroup.Options.First(o => o.Slug == "oat");

        var stockItem = new StockItem(Guid.NewGuid(), $"Test Beans {Guid.NewGuid()}", StockUnit.Gram, reorderPoint: 200, initialQuantityOnHand: 1000);
        var recipe = Recipe.ForProduct(Guid.NewGuid(), product.Id, stockItem.Id, quantityPerUnit: 18);
        dbContext.StockItems.Add(stockItem);
        dbContext.Recipes.Add(recipe);
        await dbContext.SaveChangesAsync();

        var idempotencyKey = Guid.NewGuid().ToString();
        var createResult = await createOrderHandler.HandleAsync(
            new CreateOrderRequest(
                "Integration Test", "0100000000", OrderMode.Pickup, null, null, null,
                PaymentMethod.Card, idempotencyKey,
                [new CreateOrderLineRequest(product.Id, oatMilk.Id, null, [], 1)], null),
            CancellationToken.None);

        try
        {
            Assert.True(createResult.IsSuccess);
            Assert.Equal("PendingPayment", createResult.Value.Status);

            var body = BuildBody(transactionId: 777, amountCents: (long)(createResult.Value.Total * 100), success: true, createResult.Value.Id.ToString());
            var hmac = ComputeHmac(777, (long)(createResult.Value.Total * 100), success: true);

            var webhookResult = await webhookHandler.HandleAsync(body, hmac, CancellationToken.None);
            Assert.True(webhookResult.IsSuccess, webhookResult.IsFailure ? $"{webhookResult.Error!.Code}: {webhookResult.Error.Message}" : "");

            var order = await dbContext.Orders.AsNoTracking().FirstAsync(o => o.Id == createResult.Value.Id);
            Assert.Equal(OrderStatus.Confirmed, order.Status);
            Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);

            var afterWebhook = await dbContext.StockItems.AsNoTracking().FirstAsync(s => s.Id == stockItem.Id);
            Assert.Equal(18, afterWebhook.QuantityReserved);

            // A retried delivery of the same gateway transaction id must not double-reserve.
            var repeatResult = await webhookHandler.HandleAsync(body, hmac, CancellationToken.None);
            Assert.True(repeatResult.IsSuccess);
            var afterRepeat = await dbContext.StockItems.AsNoTracking().FirstAsync(s => s.Id == stockItem.Id);
            Assert.Equal(18, afterRepeat.QuantityReserved);
        }
        finally
        {
            await CleanupAsync(dbContext, idempotencyKey, recipe.Id, stockItem.Id);
        }
    }

    [Fact]
    public async Task FailedWebhook_CancelsOrder()
    {
        await using var provider = TestServiceProviderFactory.Build(HmacSecret);
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var createOrderHandler = scope.ServiceProvider.GetRequiredService<CreateOrderHandler>();
        var webhookHandler = scope.ServiceProvider.GetRequiredService<HandlePaymentWebhookHandler>();

        var product = await dbContext.Products.AsNoTracking().FirstAsync(p => p.CategorySlug == "black" && p.IsActive);
        var milkGroup = await dbContext.ModifierGroups.AsNoTracking().Include(g => g.Options).FirstAsync(g => g.Slug == "milk");
        var noMilk = milkGroup.Options.First(o => o.Slug == "none");

        var idempotencyKey = Guid.NewGuid().ToString();
        var createResult = await createOrderHandler.HandleAsync(
            new CreateOrderRequest(
                "Integration Test", "0100000000", OrderMode.Pickup, null, null, null,
                PaymentMethod.Wallet, idempotencyKey,
                [new CreateOrderLineRequest(product.Id, noMilk.Id, null, [], 1)], null),
            CancellationToken.None);

        try
        {
            Assert.True(createResult.IsSuccess);

            var body = BuildBody(778, (long)(createResult.Value.Total * 100), success: false, createResult.Value.Id.ToString());
            var hmac = ComputeHmac(778, (long)(createResult.Value.Total * 100), success: false);

            var webhookResult = await webhookHandler.HandleAsync(body, hmac, CancellationToken.None);
            Assert.True(webhookResult.IsSuccess);

            var order = await dbContext.Orders.AsNoTracking().FirstAsync(o => o.Id == createResult.Value.Id);
            Assert.Equal(OrderStatus.Cancelled, order.Status);
            Assert.Equal(PaymentStatus.Failed, order.PaymentStatus);
        }
        finally
        {
            await CleanupAsync(dbContext, idempotencyKey, null, null);
        }
    }

    private static string BuildBody(long transactionId, long amountCents, bool success, string merchantOrderId) =>
        "{\"type\":\"TRANSACTION\",\"obj\":{" +
        $"\"amount_cents\":{amountCents}," +
        "\"created_at\":\"2026-01-01T00:00:00Z\",\"currency\":\"EGP\",\"error_occured\":false,\"has_parent_transaction\":false," +
        $"\"id\":{transactionId}," +
        "\"integration_id\":1,\"is_3d_secure\":true,\"is_auth\":false,\"is_capture\":false,\"is_refunded\":false,\"is_standalone_payment\":true,\"is_voided\":false," +
        $"\"order\":{{\"id\":9999,\"merchant_order_id\":\"{merchantOrderId}\"}}," +
        "\"owner\":123,\"pending\":false,\"source_data\":{\"pan\":\"1234\",\"sub_type\":\"Visa\",\"type\":\"card\"}," +
        $"\"success\":{(success ? "true" : "false")}" +
        "}}";

    private static string ComputeHmac(long transactionId, long amountCents, bool success)
    {
        var concatenated =
            $"{amountCents}" + "2026-01-01T00:00:00Z" + "EGP" + "false" + "false" +
            $"{transactionId}" + "1" + "true" + "false" + "false" + "false" + "true" + "false" +
            "9999" + "123" + "false" + "1234" + "Visa" + "card" + (success ? "true" : "false");

        var hash = HMACSHA512.HashData(Encoding.UTF8.GetBytes(HmacSecret), Encoding.UTF8.GetBytes(concatenated));
        return Convert.ToHexString(hash);
    }

    private static async Task CleanupAsync(CrowdCoffeeDbContext dbContext, string idempotencyKey, Guid? recipeId, Guid? stockItemId)
    {
        var orderId = await dbContext.Orders.Where(o => o.IdempotencyKey == idempotencyKey).Select(o => (Guid?)o.Id).FirstOrDefaultAsync();
        if (orderId is { } id)
        {
            await dbContext.PaymentEvents.Where(e => e.OrderId == id).ExecuteDeleteAsync();

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
