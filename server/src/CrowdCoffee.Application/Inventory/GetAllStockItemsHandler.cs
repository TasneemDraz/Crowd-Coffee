namespace CrowdCoffee.Application.Inventory;

/// Story 13.3's "on-hand vs. reserved" list — unlike GetLowStockItemsHandler (Epic 4.5), this
/// returns every stock item, not just the ones below ReorderPoint.
public class GetAllStockItemsHandler(IStockRepository repository)
{
    public async Task<IReadOnlyList<StockItemSummaryDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var items = await repository.GetAllStockItemsAsync(cancellationToken);
        return items
            .Select(s => new StockItemSummaryDto(
                s.Id, s.Name, s.Unit.ToString(), s.QuantityOnHand, s.QuantityReserved, s.Available,
                s.ReorderPoint, s.IsActive, s.QuantityOnHand < s.ReorderPoint))
            .OrderBy(s => s.Name)
            .ToList();
    }
}
