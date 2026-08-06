namespace CrowdCoffee.Infrastructure.Configuration;

/// <summary>
/// §12: secrets in environment/Key Vault, never committed. Every value here defaults to empty in
/// appsettings.json — safe to commit, since empty means "not configured" (PaymobGateway fails
/// fast with a clear error rather than calling Paymob with blank credentials). Production
/// supplies real values via environment variables (Paymob__ApiKey, etc.) or Key Vault, per
/// Story 10.1's T10.1.3.
/// </summary>
public class PaymobOptions
{
    public const string SectionName = "Paymob";

    public string ApiKey { get; init; } = string.Empty;

    public int IntegrationId { get; init; }

    public string IframeId { get; init; } = string.Empty;

    public string HmacSecret { get; init; } = string.Empty;

    public string ApiBaseUrl { get; init; } = "https://accept.paymob.com";

    /// This API's own public base URL, used to build the webhook callback URL Paymob calls back
    /// to and the return URL the customer's browser is redirected to after paying. No default —
    /// there's no sane guess for where this API is actually deployed.
    public string PublicApiBaseUrl { get; init; } = string.Empty;

    /// Where the SPA lives — the return URL sends the browser back here, not to the API.
    public string PublicFrontendBaseUrl { get; init; } = string.Empty;
}
