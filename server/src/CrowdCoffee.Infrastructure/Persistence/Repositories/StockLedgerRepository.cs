using CrowdCoffee.Application.Inventory;
using CrowdCoffee.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace CrowdCoffee.Infrastructure.Persistence.Repositories;

public class StockLedgerRepository(CrowdCoffeeDbContext dbContext) : IStockLedgerRepository
{
    public async Task<IReadOnlyList<StockItem>> GetAllStockItemsAsync(CancellationToken cancellationToken) =>
        await dbContext.StockItems.ToListAsync(cancellationToken);

    public async Task<decimal> SumMovementsAsync(Guid stockItemId, CancellationToken cancellationToken) =>
        await dbContext.StockMovements
            .Where(m => m.StockItemId == stockItemId)
            .SumAsync(m => (decimal?)m.Quantity, cancellationToken) ?? 0m;

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
