using CrowdCoffee.Api.Common;
using CrowdCoffee.Application.Inventory;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CrowdCoffee.Api.Controllers;

// TODO(Epic 11): restrict to [Authorize(Roles = "Manager")] once staff auth exists.
[ApiController]
[Route("api/inventory")]
public class InventoryController(
    ReconcileStockLedgerHandler reconcileHandler,
    GetLowStockItemsHandler lowStockHandler,
    RecordPurchaseHandler purchaseHandler,
    IValidator<RecordPurchaseRequest> purchaseValidator,
    RecordWasteHandler wasteHandler,
    IValidator<RecordWasteRequest> wasteValidator,
    RecordAdjustmentHandler adjustmentHandler,
    IValidator<RecordAdjustmentRequest> adjustmentValidator,
    StockReservationService reservationService) : ControllerBase
{
    [HttpPost("reconcile-stock-ledger")]
    public async Task<IActionResult> ReconcileStockLedger(CancellationToken cancellationToken)
    {
        var result = await reconcileHandler.HandleAsync(cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockItems(CancellationToken cancellationToken)
    {
        var items = await lowStockHandler.HandleAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("stock/purchase")]
    public async Task<IActionResult> RecordPurchase(RecordPurchaseRequest request, CancellationToken cancellationToken)
    {
        var validation = await purchaseValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var failure in validation.Errors)
            {
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var result = await purchaseHandler.HandleAsync(request, DateTime.UtcNow, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("stock/waste")]
    public async Task<IActionResult> RecordWaste(RecordWasteRequest request, CancellationToken cancellationToken)
    {
        var validation = await wasteValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var failure in validation.Errors)
            {
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var result = await wasteHandler.HandleAsync(request, DateTime.UtcNow, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("stock/adjustment")]
    public async Task<IActionResult> RecordAdjustment(RecordAdjustmentRequest request, CancellationToken cancellationToken)
    {
        var validation = await adjustmentValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var failure in validation.Errors)
            {
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var result = await adjustmentHandler.HandleAsync(request, DateTime.UtcNow, cancellationToken);
        return result.ToActionResult(this);
    }

    // Epic 9/12 call StockReservationService directly, in-process, at checkout confirmation and
    // order completion — these endpoints exist for ops visibility/manual correction, not as the
    // primary call path.
    [HttpPost("orders/{orderId:guid}/reserve")]
    public async Task<IActionResult> ReserveForOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await reservationService.ReserveForOrderAsync(orderId, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("orders/{orderId:guid}/release")]
    public async Task<IActionResult> ReleaseForOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await reservationService.ReleaseForOrderAsync(orderId, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("orders/{orderId:guid}/fulfill")]
    public async Task<IActionResult> FulfillForOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await reservationService.FulfillForOrderAsync(orderId, "manual-api", DateTime.UtcNow, cancellationToken);
        return result.ToActionResult(this);
    }
}
