using CrowdCoffee.Api.Common;
using CrowdCoffee.Application.Auth;
using CrowdCoffee.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CrowdCoffee.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    LoginHandler loginHandler,
    IValidator<LoginRequest> loginValidator,
    RefreshHandler refreshHandler,
    LogoutHandler logoutHandler) : ControllerBase
{
    // Scoped to /api/auth so the cookie is never sent to unrelated endpoints; httpOnly/SameSite
    // so it's invisible to page JS and not attached to cross-site requests (Story 11.1/11.2).
    private const string RefreshCookieName = "crowdcoffee_refresh_token";

    [HttpPost("login")]
    [EnableRateLimiting("staff-login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var validation = await loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var failure in validation.Errors)
            {
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var result = await loginHandler.HandleAsync(request, DateTime.UtcNow, cancellationToken);
        return ToAuthActionResult(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var rawToken = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(rawToken))
        {
            return Unauthorized();
        }

        var result = await refreshHandler.HandleAsync(rawToken, DateTime.UtcNow, cancellationToken);
        if (result.IsFailure)
        {
            Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = "/api/auth" });
        }

        return ToAuthActionResult(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var rawToken = Request.Cookies[RefreshCookieName];
        if (!string.IsNullOrEmpty(rawToken))
        {
            await logoutHandler.HandleAsync(rawToken, DateTime.UtcNow, cancellationToken);
        }

        Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = "/api/auth" });
        return NoContent();
    }

    private IActionResult ToAuthActionResult(Result<AuthResult> result)
    {
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        var auth = result.Value;

        // Secure mirrors whether this request itself arrived over HTTPS — local dev runs the API
        // over plain HTTP (see launchSettings' "http" profile) where a Secure cookie would
        // silently never be sent by the browser; production terminates TLS, so Request.IsHttps
        // is true there and the cookie gets the real protection.
        Response.Cookies.Append(RefreshCookieName, auth.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = auth.RefreshTokenExpiresAtUtc,
            Path = "/api/auth"
        });

        return Ok(new
        {
            accessToken = auth.AccessToken,
            expiresAt = auth.AccessTokenExpiresAtUtc,
            username = auth.Username,
            role = auth.Role
        });
    }
}
