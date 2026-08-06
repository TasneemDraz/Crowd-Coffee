namespace CrowdCoffee.Infrastructure.Persistence;

/// <summary>
/// Backs SqlOrderNumberGenerator (T9.2.1). Deliberately an Infrastructure-only type, not a
/// Domain entity — this is a technical sequence-generation concern, not a business concept, and
/// nothing outside order-number formatting ever reads it.
/// </summary>
public class OrderNumberCounter
{
    /// yyMMdd, Africa/Cairo-local (CairoClock.Today) — the PK, so the day's counter is created on
    /// first use rather than provisioned in advance.
    public string DateKey { get; set; } = string.Empty;

    public int LastValue { get; set; }
}
