using CrowdCoffee.Domain.Common;
using Microsoft.Extensions.Logging;

namespace CrowdCoffee.Application.Inventory;

/// <summary>
/// PRD §6 rule 3: StockMovement is the source of truth, QuantityOnHand is a cached projection.
/// This recomputes the cache from the ledger for every StockItem and reports (and fixes) drift.
/// </summary>
public class ReconcileStockLedgerHandler(IStockLedgerRepository repository, ILogger<ReconcileStockLedgerHandler> logger)
{
    public async Task<Result<ReconcileReport>> HandleAsync(CancellationToken cancellationToken)
    {
        var stockItems = await repository.GetAllStockItemsAsync(cancellationToken);
        var results = new List<StockItemReconcileResult>();

        foreach (var stockItem in stockItems)
        {
            var recomputed = await repository.SumMovementsAsync(stockItem.Id, cancellationToken);
            var previous = stockItem.QuantityOnHand;
            var drift = recomputed - previous;

            if (drift != 0)
            {
                logger.LogWarning(
                    "Stock drift detected for {StockItemName} ({StockItemId}): cached={Previous}, ledger={Recomputed}, drift={Drift}",
                    stockItem.Name, stockItem.Id, previous, recomputed, drift);
                stockItem.Reconcile(recomputed);
            }

            results.Add(new StockItemReconcileResult(stockItem.Id, stockItem.Name, previous, recomputed, drift));
        }

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success(new ReconcileReport(results));
    }
}
