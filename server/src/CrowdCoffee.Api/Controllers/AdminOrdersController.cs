using CrowdCoffee.Api.Common;
using CrowdCoffee.Application.Ordering;
using CrowdCoffee.Domain.Ordering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdCoffee.Api.Controllers;

[Authorize(Policy = "ManagerOnly")]
[ApiController]
[Route("api/admin/orders")]
public class AdminOrdersController(SearchOrdersHandler searchHandler, GetOrderDetailHandler detailHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] OrderStatus? status,
        [FromQuery] OrderMode? mode,
        [FromQuery] PaymentMethod? paymentMethod,
        CancellationToken cancellationToken)
    {
        var filters = new OrderSearchFilters(dateFrom, dateTo, status, mode, paymentMethod);
        var orders = await searchHandler.HandleAsync(filters, cancellationToken);
        return Ok(orders);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetDetail(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await detailHandler.HandleAsync(orderId, cancellationToken);
        return result.ToActionResult(this);
    }
}
