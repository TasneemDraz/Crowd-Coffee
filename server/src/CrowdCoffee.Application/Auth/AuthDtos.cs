namespace CrowdCoffee.Application.Auth;

public record LoginRequest(string Username, string Password);

/// RefreshToken/RefreshTokenExpiresAtUtc are the raw cookie value + its expiry — the controller
/// sets the httpOnly cookie from these and never puts them in the JSON response body.
public record AuthResult(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    string Username,
    string Role);
