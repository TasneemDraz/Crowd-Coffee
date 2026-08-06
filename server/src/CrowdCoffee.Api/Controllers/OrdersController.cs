using CrowdCoffee.Api.Common;
using CrowdCoffee.Application.Ordering;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CrowdCoffee.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(
    CreateOrderHandler createOrderHandler,
    IValidator<CreateOrderRequest> createOrderValidator,
    GetOrderHandler getOrderHandler) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("order-creation")]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var validation = await createOrderValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var failure in validation.Errors)
            {
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var result = await createOrderHandler.HandleAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }

    /// Not customer-authenticated (no staff/customer auth exists yet, Epic 11) — the order id is
    /// an unguessable Guid, and the response carries nothing more sensitive than what the
    /// customer already saw on the confirmation screen.
    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetById(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await getOrderHandler.HandleAsync(orderId, cancellationToken);
        return result.ToActionResult(this);
    }
}
