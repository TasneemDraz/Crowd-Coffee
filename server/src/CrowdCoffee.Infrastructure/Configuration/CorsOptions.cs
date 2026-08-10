namespace CrowdCoffee.Infrastructure.Configuration;

/// <summary>
/// Story 16.1 (T16.1.2): explicit allow-list, never a wildcard — the refresh-token cookie
/// (Epic 11) rides on credentialed cross-origin requests once the API and the Vue SPA are
/// deployed as separate origins (Epic 17's Docker setup), and browsers reject
/// Access-Control-Allow-Origin: * combined with credentials anyway. Local dev default covers
/// the Vite dev server only; production supplies its real origin(s) via the
/// Cors__AllowedOrigins__0 environment variable, same override pattern as Jwt/Database/Paymob.
/// </summary>
public class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "AllowedOrigins";

    public string[] AllowedOrigins { get; set; } = [];
}
