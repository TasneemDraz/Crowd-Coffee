namespace CrowdCoffee.Application.Inventory;

public record StockItemReconcileResult(Guid StockItemId, string Name, decimal PreviousQuantityOnHand, decimal RecomputedQuantityOnHand, decimal Drift);

public record ReconcileReport(IReadOnlyList<StockItemReconcileResult> Items)
{
    public int DriftedCount => Items.Count(i => i.Drift != 0);
}
