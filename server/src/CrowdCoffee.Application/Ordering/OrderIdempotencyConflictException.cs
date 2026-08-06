namespace CrowdCoffee.Application.Ordering;

/// <summary>
/// Thrown by IOrderRepository.SaveChangesAsync when the DB rejects an insert on the unique
/// IdempotencyKey index (Story 9.3) — a genuinely concurrent double-submit, not a bug. Kept as a
/// plain Application-layer exception (not a Result) since it surfaces from EF's SaveChanges deep
/// inside the repository call, and translating it to Result there would require the Infrastructure
/// implementation to inspect provider-specific SqlException numbers just to hand back a type
/// Application already knows about — cleaner for Infrastructure to do that translation once and
/// throw this instead of leaking DbUpdateException across the layer boundary.
/// </summary>
public sealed class OrderIdempotencyConflictException : Exception;
