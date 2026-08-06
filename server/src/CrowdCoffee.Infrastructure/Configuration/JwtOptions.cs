namespace CrowdCoffee.Infrastructure.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// Local-dev default committed here is a placeholder, not a production secret (same pattern
    /// as Database:ConnectionString) — production overrides via the Jwt__SigningKey environment
    /// variable, and this value should be rotated before go-live.
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
