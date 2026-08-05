using CrowdCoffee.Api.Common;
using CrowdCoffee.Application.Cart;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CrowdCoffee.Api.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController(RepriceCartHandler repriceHandler, IValidator<CartRepriceRequest> repriceValidator) : ControllerBase
{
    [HttpPost("reprice")]
    public async Task<IActionResult> Reprice(CartRepriceRequest request, CancellationToken cancellationToken)
    {
        var validation = await repriceValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var failure in validation.Errors)
            {
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var result = await repriceHandler.HandleAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }
}
