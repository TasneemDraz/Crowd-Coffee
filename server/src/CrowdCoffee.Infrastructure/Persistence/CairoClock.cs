namespace CrowdCoffee.Infrastructure.Persistence;

/// <summary>
/// §6 rule 5: timestamps are stored UTC, rendered in Africa/Cairo. The order-number sequence
/// (§6 rule 2, CRW-YYMMDD-NNNN) resets on the shop's local day, not the UTC day — an order placed
/// at 11pm Cairo time is still "today" to the shop even though it's already tomorrow in UTC.
/// Tries the IANA id first (works on Linux CI/containers), then the Windows id (local dev) —
/// .NET's cross-platform ICU lookup usually bridges these but isn't guaranteed on every install.
/// </summary>
public static class CairoClock
{
    private static readonly TimeZoneInfo TimeZone = ResolveTimeZone();

    public static DateOnly Today(DateTime utcNow) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZone));

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        }
    }
}
