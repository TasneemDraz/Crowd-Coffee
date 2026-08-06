using CrowdCoffee.Domain.Inventory;

namespace CrowdCoffee.Application.Inventory;

/// <summary>
/// Port for StockItem/StockMovement access. Started as IStockLedgerRepository (Epic 2, Story
/// 2.6) with just the reconcile command's needs; broadened and renamed here since Epic 4 adds
/// several more StockItem-centric operations that don't belong under "ledger" specifically
/// (reservation lookups, low-stock queries). Consolidating now, while there's still only one
/// implementation and one consumer, is cheaper than letting near-duplicate repositories accrete.
/// </summary>
public interface IStockRepository
{
    Task<StockItem?> GetByIdAsync(Guid stockItemId, CancellationToken cancellationToken);

    Task<IReadOnlyList<StockItem>> GetAllStockItemsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<StockItem>> GetStockItemsAsync(IReadOnlyCollection<Guid> stockItemIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<StockItem>> GetLowStockItemsAsync(CancellationToken cancellationToken);

    /// Story 13.3's per-item ledger view. Newest first, capped — a shop-floor admin screen needs
    /// recent activity, not the entire history of a years-old ingredient.
    Task<IReadOnlyList<StockMovement>> GetMovementsForStockItemAsync(Guid stockItemId, int take, CancellationToken cancellationToken);

    Task<decimal> SumMovementsAsync(Guid stockItemId, CancellationToken cancellationToken);

    /// Backs the double-fulfillment guard (Story 4.2): has a Sale movement already been recorded
    /// for this order?
    Task<bool> HasMovementForReferenceAsync(string referenceType, Guid referenceId, StockMovementType movementType, CancellationToken cancellationToken);

    void AddStockMovement(StockMovement movement);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
