using CrowdCoffee.Application.Inventory;
using CrowdCoffee.Domain.Inventory;
using CrowdCoffee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CrowdCoffee.Infrastructure.Tests.Inventory;

/// Story 13.3 against real local SQL Server: the on-hand/reserved/available list and the
/// per-item movement ledger.
public class StockAdminIntegrationTests
{
    [Fact]
    public async Task GetAllStockItems_IncludesOnHandReservedAndAvailable_ForARealItem()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<GetAllStockItemsHandler>();

        var stockItem = new StockItem(Guid.NewGuid(), $"Test Beans {Guid.NewGuid()}", StockUnit.Gram, reorderPoint: 500, initialQuantityOnHand: 300);
        dbContext.StockItems.Add(stockItem);
        await dbContext.SaveChangesAsync();

        try
        {
            var items = await handler.HandleAsync(CancellationToken.None);
            var row = Assert.Single(items.Where(i => i.Id == stockItem.Id));

            Assert.Equal(300, row.QuantityOnHand);
            Assert.Equal(0, row.QuantityReserved);
            Assert.Equal(300, row.Available);
            Assert.True(row.IsLowStock); // 300 < reorderPoint 500
        }
        finally
        {
            await dbContext.StockItems.Where(s => s.Id == stockItem.Id).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task GetStockMovements_ReturnsPurchaseAndWasteEntries_NewestFirst()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var purchaseHandler = scope.ServiceProvider.GetRequiredService<RecordPurchaseHandler>();
        var wasteHandler = scope.ServiceProvider.GetRequiredService<RecordWasteHandler>();
        var ledgerHandler = scope.ServiceProvider.GetRequiredService<GetStockMovementsHandler>();

        var stockItem = new StockItem(Guid.NewGuid(), $"Test Beans {Guid.NewGuid()}", StockUnit.Gram, reorderPoint: 10, initialQuantityOnHand: 100);
        dbContext.StockItems.Add(stockItem);
        await dbContext.SaveChangesAsync();

        try
        {
            var now = DateTime.UtcNow;
            await purchaseHandler.HandleAsync(new RecordPurchaseRequest(stockItem.Id, 50, "manager1", "Weekly delivery"), now, CancellationToken.None);
            await wasteHandler.HandleAsync(new RecordWasteRequest(stockItem.Id, 5, "manager1", "Spilled"), now.AddSeconds(1), CancellationToken.None);

            var result = await ledgerHandler.HandleAsync(stockItem.Id, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            Assert.Equal("Waste", result.Value[0].MovementType); // newest first
            Assert.Equal(-5, result.Value[0].Quantity);
            Assert.Equal("Purchase", result.Value[1].MovementType);
            Assert.Equal(50, result.Value[1].Quantity);
        }
        finally
        {
            await dbContext.StockMovements.Where(m => m.StockItemId == stockItem.Id).ExecuteDeleteAsync();
            await dbContext.StockItems.Where(s => s.Id == stockItem.Id).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task GetStockMovements_UnknownStockItem_ReturnsNotFound()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var ledgerHandler = scope.ServiceProvider.GetRequiredService<GetStockMovementsHandler>();

        var result = await ledgerHandler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("StockItem.NotFound", result.Error!.Code);
    }
}
