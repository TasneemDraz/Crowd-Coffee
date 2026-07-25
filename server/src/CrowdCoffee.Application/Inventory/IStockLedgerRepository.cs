using CrowdCoffee.Domain.Inventory;

namespace CrowdCoffee.Application.Inventory;

public interface IStockLedgerRepository
{
    Task<IReadOnlyList<StockItem>> GetAllStockItemsAsync(CancellationToken cancellationToken);

    Task<decimal> SumMovementsAsync(Guid stockItemId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
