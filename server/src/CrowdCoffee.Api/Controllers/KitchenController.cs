using CrowdCoffee.Api.Common;
using CrowdCoffee.Application.Kitchen;
using CrowdCoffee.Application.Ordering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdCoffee.Api.Controllers;

[Authorize(Policy = "StaffOnly")]
[ApiController]
[Route("api/kitchen")]
public class KitchenController(GetKitchenBoardOrdersHandler getBoardHandler, AdvanceOrderStatusHandler advanceHandler) : ControllerBase
{
    [HttpGet("orders")]
    public async Task<IActionResult> GetBoard(CancellationToken cancellationToken)
    {
        var orders = await getBoardHandler.HandleAsync(cancellationToken);
        return Ok(orders);
    }

    [HttpPost("orders/{orderId:guid}/advance")]
    public async Task<IActionResult> Advance(Guid orderId, CancellationToken cancellationToken)
    {
        var changedBy = User.Identity?.Name ?? "staff";
        var result = await advanceHandler.HandleAsync(orderId, changedBy, DateTime.UtcNow, cancellationToken);
        return result.ToActionResult(this);
    }
}
