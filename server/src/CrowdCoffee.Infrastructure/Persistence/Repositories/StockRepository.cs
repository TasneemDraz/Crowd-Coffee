using CrowdCoffee.Application.Inventory;
using CrowdCoffee.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace CrowdCoffee.Infrastructure.Persistence.Repositories;

public class StockRepository(CrowdCoffeeDbContext dbContext) : IStockRepository
{
    public async Task<StockItem?> GetByIdAsync(Guid stockItemId, CancellationToken cancellationToken) =>
        await dbContext.StockItems.FirstOrDefaultAsync(s => s.Id == stockItemId, cancellationToken);

    public async Task<IReadOnlyList<StockItem>> GetAllStockItemsAsync(CancellationToken cancellationToken) =>
        await dbContext.StockItems.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StockItem>> GetStockItemsAsync(IReadOnlyCollection<Guid> stockItemIds, CancellationToken cancellationToken) =>
        await dbContext.StockItems.Where(s => stockItemIds.Contains(s.Id)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StockItem>> GetLowStockItemsAsync(CancellationToken cancellationToken) =>
        await dbContext.StockItems
            .Where(s => s.IsActive && s.QuantityOnHand < s.ReorderPoint)
            .ToListAsync(cancellationToken);

    public async Task<decimal> SumMovementsAsync(Guid stockItemId, CancellationToken cancellationToken) =>
        await dbContext.StockMovements
            .Where(m => m.StockItemId == stockItemId)
            .SumAsync(m => (decimal?)m.Quantity, cancellationToken) ?? 0m;

    public async Task<bool> HasMovementForReferenceAsync(string referenceType, Guid referenceId, StockMovementType movementType, CancellationToken cancellationToken) =>
        await dbContext.StockMovements.AnyAsync(
            m => m.ReferenceType == referenceType && m.ReferenceId == referenceId && m.MovementType == movementType,
            cancellationToken);

    public void AddStockMovement(StockMovement movement) => dbContext.StockMovements.Add(movement);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
