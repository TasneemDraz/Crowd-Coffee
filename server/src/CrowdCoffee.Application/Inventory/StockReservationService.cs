using CrowdCoffee.Domain.Common;
using CrowdCoffee.Domain.Inventory;
using Microsoft.Extensions.Logging;

namespace CrowdCoffee.Application.Inventory;

/// <summary>
/// Orchestrates §8's reservation flow. Epic 9 (checkout) calls ReserveForOrderAsync on
/// Confirmed; Epic 12 (kitchen board) calls FulfillForOrderAsync on Delivered/PickedUp; the
/// OrderCancelled domain event handler calls ReleaseForOrderAsync. None of that wiring exists
/// yet — this service is the callable capability those epics will invoke directly, in-process.
/// </summary>
public class StockReservationService(
    IOrderStockRequirementRepository requirementRepository,
    IStockRepository stockRepository,
    ILogger<StockReservationService> logger)
{
    /// On order confirmation. Validates every required ingredient has enough Available stock
    /// *before* reserving any of them — a two-pass check-then-commit, so a failure partway
    /// through a multi-ingredient order never leaves a partial reservation behind (§12).
    public async Task<Result> ReserveForOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var requirements = await LoadRequirementsAsync(orderId, cancellationToken);
        if (requirements is null)
        {
            return Result.Failure(Error.NotFound("Order.NotFound", $"Order {orderId} not found."));
        }

        if (requirements.Count == 0)
        {
            return Result.Success();
        }

        var stockItemsById = (await stockRepository.GetStockItemsAsync(requirements.Keys.ToList(), cancellationToken))
            .ToDictionary(s => s.Id);

        foreach (var (stockItemId, quantity) in requirements)
        {
            if (!stockItemsById.TryGetValue(stockItemId, out var stockItem))
            {
                return Result.Failure(Error.Conflict("Stock.MissingStockItem", $"Recipe references StockItem {stockItemId}, which does not exist."));
            }

            if (stockItem.Available < quantity)
            {
                return Result.Failure(Error.Conflict(
                    "Stock.InsufficientStock",
                    $"Insufficient stock for {stockItem.Name}: order needs {quantity}, available {stockItem.Available}."));
            }
        }

        foreach (var (stockItemId, quantity) in requirements)
        {
            stockItemsById[stockItemId].Reserve(quantity);
        }

        await stockRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// On cancellation. Best-effort by design (unlike Reserve): releasing what we can is
    /// strictly better than releasing nothing because one line had an accounting discrepancy,
    /// and the order is already Cancelled regardless of how this turns out.
    public async Task<Result> ReleaseForOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var requirements = await LoadRequirementsAsync(orderId, cancellationToken);
        if (requirements is null)
        {
            return Result.Failure(Error.NotFound("Order.NotFound", $"Order {orderId} not found."));
        }

        if (requirements.Count == 0)
        {
            return Result.Success();
        }

        var stockItemsById = (await stockRepository.GetStockItemsAsync(requirements.Keys.ToList(), cancellationToken))
            .ToDictionary(s => s.Id);

        foreach (var (stockItemId, quantity) in requirements)
        {
            if (stockItemsById.TryGetValue(stockItemId, out var stockItem))
            {
                stockItem.ReleaseReservation(quantity);
            }
            else
            {
                logger.LogWarning("Cannot release reservation for missing StockItem {StockItemId} (order {OrderId}).", stockItemId, orderId);
            }
        }

        await stockRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// On order completion (Delivered/PickedUp). Idempotent — a second call for the same order
    /// is a no-op, guarding against double-fulfillment (Story 4.2's explicit requirement) if the
    /// completion transition is somehow triggered twice.
    public async Task<Result> FulfillForOrderAsync(Guid orderId, string performedBy, DateTime occurredAtUtc, CancellationToken cancellationToken)
    {
        if (await stockRepository.HasMovementForReferenceAsync("Order", orderId, StockMovementType.Sale, cancellationToken))
        {
            return Result.Success();
        }

        var requirements = await LoadRequirementsAsync(orderId, cancellationToken);
        if (requirements is null)
        {
            return Result.Failure(Error.NotFound("Order.NotFound", $"Order {orderId} not found."));
        }

        if (requirements.Count == 0)
        {
            return Result.Success();
        }

        var stockItemsById = (await stockRepository.GetStockItemsAsync(requirements.Keys.ToList(), cancellationToken))
            .ToDictionary(s => s.Id);

        foreach (var (stockItemId, quantity) in requirements)
        {
            if (!stockItemsById.TryGetValue(stockItemId, out var stockItem))
            {
                return Result.Failure(Error.Conflict("Stock.MissingStockItem", $"Recipe references StockItem {stockItemId}, which does not exist."));
            }

            if (stockItem.QuantityReserved < quantity)
            {
                return Result.Failure(Error.Conflict(
                    "Stock.InsufficientReservation",
                    $"Cannot fulfill {stockItem.Name}: order needs {quantity}, only {stockItem.QuantityReserved} reserved."));
            }
        }

        foreach (var (stockItemId, quantity) in requirements)
        {
            stockItemsById[stockItemId].Fulfill(quantity);
            stockRepository.AddStockMovement(new StockMovement(
                Guid.NewGuid(), stockItemId, -quantity, StockMovementType.Sale, performedBy, occurredAtUtc,
                referenceType: "Order", referenceId: orderId));
        }

        await stockRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<IReadOnlyDictionary<Guid, decimal>?> LoadRequirementsAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await requirementRepository.GetOrderWithItemsAsync(orderId, cancellationToken);
        return order is null ? null : await requirementRepository.GetRequiredQuantitiesAsync(order, cancellationToken);
    }
}
