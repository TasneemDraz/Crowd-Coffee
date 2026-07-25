using CrowdCoffee.Api.Common;
using CrowdCoffee.Application.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace CrowdCoffee.Api.Controllers;

// TODO(Epic 11): restrict to [Authorize(Roles = "Manager")] once staff auth exists.
[ApiController]
[Route("api/inventory")]
public class InventoryController(ReconcileStockLedgerHandler handler) : ControllerBase
{
    [HttpPost("reconcile-stock-ledger")]
    public async Task<IActionResult> ReconcileStockLedger(CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return result.ToActionResult(this);
    }
}
