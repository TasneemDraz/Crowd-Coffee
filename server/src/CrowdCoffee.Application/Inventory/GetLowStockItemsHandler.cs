using CrowdCoffee.Domain.Inventory;

namespace CrowdCoffee.Application.Inventory;

public record LowStockItem(Guid StockItemId, string Name, decimal QuantityOnHand, decimal ReorderPoint);

/// <summary>
/// §8/§11: surfaces StockItems below ReorderPoint for the admin dashboard (Epic 14). Compares
/// QuantityOnHand, not Available — "drops below ReorderPoint" reads most naturally as a
/// physical-stock signal; open to revisiting against Available (on-hand minus reserved) if the
/// business wants reorder timing to account for stock already committed to pending orders.
///
/// Who gets proactively notified and how (§14.6) is still an open question — this only builds
/// the queryable list, no push/email/SMS.
/// </summary>
public class GetLowStockItemsHandler(IStockRepository repository)
{
    public async Task<IReadOnlyList<LowStockItem>> HandleAsync(CancellationToken cancellationToken)
    {
        var stockItems = await repository.GetLowStockItemsAsync(cancellationToken);
        return stockItems
            .Select(s => new LowStockItem(s.Id, s.Name, s.QuantityOnHand, s.ReorderPoint))
            .ToList();
    }
}
