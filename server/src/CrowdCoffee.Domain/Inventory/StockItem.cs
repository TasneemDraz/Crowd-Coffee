using CrowdCoffee.Domain.Common;

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
    /// the Reserve/Fulfill business methods below, which mutate this alongside writing a ledger
    /// row; this one exists precisely for when the cache has drifted from the ledger.
    public void Reconcile(decimal quantityOnHand) => QuantityOnHand = quantityOnHand;

    /// On order confirmation (§8). Writes no ledger row — reservation isn't a consumption event,
    /// just a hold against Available. Caller (StockReservationService) is responsible for
    /// validating availability across an order's *entire* ingredient set before calling Reserve
    /// on any single line, so a mid-order failure never leaves a partial reservation behind.
    public Result Reserve(decimal quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(Error.Validation("StockItem.InvalidQuantity", "Reserve quantity must be positive."));
        }

        if (Available < quantity)
        {
            return Result.Failure(Error.Conflict(
                "StockItem.InsufficientStock",
                $"Insufficient stock for {Name}: requested {quantity}, available {Available}."));
        }

        QuantityReserved += quantity;
        return Result.Success();
    }

    /// On cancellation (§8) — releases a hold without touching the ledger (nothing was ever
    /// consumed). Best-effort by design: the caller (cancellation's event handler) should keep
    /// releasing other lines even if one clamps, rather than aborting the whole release because
    /// of a single accounting discrepancy.
    public void ReleaseReservation(decimal quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        QuantityReserved = Math.Max(0, QuantityReserved - quantity);
    }

    /// On order completion (§8) — converts a reservation into actual consumption: decrements
    /// both the hold and the on-hand cache. The caller is still responsible for writing the
    /// corresponding Sale StockMovement row; this method only owns the StockItem's own invariant.
    public Result Fulfill(decimal quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(Error.Validation("StockItem.InvalidQuantity", "Fulfill quantity must be positive."));
        }

        if (QuantityReserved < quantity)
        {
            return Result.Failure(Error.Conflict(
                "StockItem.InsufficientReservation",
                $"Cannot fulfill {quantity} of {Name}: only {QuantityReserved} is reserved."));
        }

        QuantityReserved -= quantity;
        QuantityOnHand -= quantity;
        return Result.Success();
    }

    /// Backs Purchase/Waste/Adjustment movements (Story 4.6) — a signed delta applied directly
    /// to on-hand, guarded so physical stock can never go negative (e.g. wasting more than is on
    /// hand, or a correction that overshoots).
    public Result AdjustOnHand(decimal delta)
    {
        var newQuantity = QuantityOnHand + delta;
        if (newQuantity < 0)
        {
            return Result.Failure(Error.Conflict(
                "StockItem.NegativeOnHand",
                $"Adjusting {Name} by {delta} would take on-hand below zero (currently {QuantityOnHand})."));
        }

        QuantityOnHand = newQuantity;
        return Result.Success();
    }
}
