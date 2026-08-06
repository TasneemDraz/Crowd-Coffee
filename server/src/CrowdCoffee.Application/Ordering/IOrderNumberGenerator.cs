namespace CrowdCoffee.Application.Ordering;

/// <summary>
/// §6 rule 2: CRW-YYMMDD-NNNN, sequence resetting daily. "Daily" means the shop's local
/// (Africa/Cairo) day, not the UTC day the server happens to be storing timestamps in — a
/// customer ordering at 11pm Cairo time / 9pm UTC should still land in that Cairo day's sequence.
/// Must be called from inside the caller's transaction: the Infrastructure implementation relies
/// on row-level locking from an UPDATE statement to stay correct under concurrent order creation
/// (T9.2.1: avoid MAX()+1 races).
/// </summary>
public interface IOrderNumberGenerator
{
    Task<string> NextAsync(DateTime nowUtc, CancellationToken cancellationToken);
}
