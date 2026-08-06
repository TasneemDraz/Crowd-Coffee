using CrowdCoffee.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdCoffee.Api.Controllers;

[Authorize(Policy = "ManagerOnly")]
[ApiController]
[Route("api/admin/dashboard")]
public class AdminDashboardController(GetDailyDashboardHandler handler) : ControllerBase
{
    [HttpGet("today")]
    public async Task<IActionResult> GetToday(CancellationToken cancellationToken)
    {
        var dashboard = await handler.HandleAsync(DateTime.UtcNow, cancellationToken);
        return Ok(dashboard);
    }
}
