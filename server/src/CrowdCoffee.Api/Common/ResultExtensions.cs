using CrowdCoffee.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CrowdCoffee.Api.Common;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(result.Value);
        }

        return controller.Problem(
            title: result.Error!.Code,
            detail: result.Error.Message,
            statusCode: StatusCodeFor(result.Error.Type));
    }

    public static IActionResult ToActionResult(this Result result, ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            return controller.NoContent();
        }

        return controller.Problem(
            title: result.Error!.Code,
            detail: result.Error.Message,
            statusCode: StatusCodeFor(result.Error.Type));
    }

    private static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        _ => StatusCodes.Status500InternalServerError
    };
}
