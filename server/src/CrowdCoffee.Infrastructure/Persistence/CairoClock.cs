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

    /// Epic 14's dashboard: the UTC instant range covering one Cairo-local calendar day, for
    /// querying "today's" orders by CreatedAt (UTC) without loading rows outside the shop's day
    /// just to filter them out in memory.
    public static (DateTime StartUtc, DateTime EndUtc) DayRangeUtc(DateOnly cairoDate)
    {
        var startLocal = DateTime.SpecifyKind(cairoDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var endLocal = DateTime.SpecifyKind(cairoDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return (TimeZoneInfo.ConvertTimeToUtc(startLocal, TimeZone), TimeZoneInfo.ConvertTimeToUtc(endLocal, TimeZone));
    }

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
