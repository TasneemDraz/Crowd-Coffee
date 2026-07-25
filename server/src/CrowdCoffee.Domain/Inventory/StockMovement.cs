namespace CrowdCoffee.Domain.Inventory;

public enum StockMovementType
{
    Purchase,
    Sale,
    Waste,
    Adjustment,
    Return
}

/// <summary>
/// Append-only ledger row (§6 rule 3). Never updated in place — StockItem.QuantityOnHand is a
/// cached projection derived from summing these; the reconcile command in Story 2.6 recomputes
/// it from this table and reports drift.
/// </summary>
public class StockMovement
{
    public Guid Id { get; private set; }
    public Guid StockItemId { get; private set; }

    /// Signed: positive for Purchase/Return, negative for Sale/Waste; Adjustment can be either.
    public decimal Quantity { get; private set; }
    public StockMovementType MovementType { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? Notes { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private StockMovement()
    {
    }

    public StockMovement(Guid id, Guid stockItemId, decimal quantity, StockMovementType movementType, string createdBy, DateTime createdAt, string? referenceType = null, Guid? referenceId = null, string? notes = null)
    {
        Id = id;
        StockItemId = stockItemId;
        Quantity = quantity;
        MovementType = movementType;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Notes = notes;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }
}
