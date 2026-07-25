namespace CrowdCoffee.Domain.Inventory;

public class StockItem
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public StockUnit Unit { get; private set; }

    /// Cached projection, not the source of truth — the StockMovement ledger is (§6 rule 3).
    /// Reservation/fulfillment logic in Epic 4 mutates this alongside writing ledger rows.
    public decimal QuantityOnHand { get; private set; }
    public decimal QuantityReserved { get; private set; }
    public decimal ReorderPoint { get; private set; }
    public bool IsActive { get; private set; } = true;

    public decimal Available => QuantityOnHand - QuantityReserved;

    private StockItem()
    {
    }

    public StockItem(Guid id, string name, StockUnit unit, decimal reorderPoint, decimal initialQuantityOnHand = 0)
    {
        Id = id;
        Name = name;
        Unit = unit;
        ReorderPoint = reorderPoint;
        QuantityOnHand = initialQuantityOnHand;
        QuantityReserved = 0;
        IsActive = true;
    }

    /// Administrative correction used by the ledger reconcile command (Story 2.6) — sets the
    /// cached projection to whatever the StockMovement ledger actually sums to. Distinct from
    /// Epic 4's Reserve/Fulfill business methods, which mutate this alongside writing a ledger
    /// row; this one exists precisely for when the cache has drifted from the ledger.
    public void Reconcile(decimal quantityOnHand) => QuantityOnHand = quantityOnHand;
}
