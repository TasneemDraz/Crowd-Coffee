using CrowdCoffee.Api.Common;
using CrowdCoffee.Application.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CrowdCoffee.Api.Controllers;

[ApiController]
[Route("api/diagnostics")]
public class DiagnosticsController(IValidator<EchoRequest> validator, EchoHandler handler) : ControllerBase
{
    [HttpPost("echo")]
    public async Task<IActionResult> Echo(EchoRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var failure in validationResult.Errors)
            {
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var result = handler.Handle(request);
        return result.ToActionResult(this);
    }
}
