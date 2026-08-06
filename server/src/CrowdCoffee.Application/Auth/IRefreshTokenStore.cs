using CrowdCoffee.Domain.Staff;

namespace CrowdCoffee.Application.Auth;

/// Hashing/lookup is an Infrastructure concern (raw token in, hash out) — this port only trades
/// in the raw value (needed for the httpOnly cookie) and the persisted RefreshToken row.
public interface IRefreshTokenStore
{
    Task<(RefreshToken Token, string RawValue)> IssueAsync(Guid staffUserId, DateTime nowUtc, CancellationToken cancellationToken);

    Task<RefreshToken?> GetByRawValueAsync(string rawValue, CancellationToken cancellationToken);

    /// Reuse-detection response to a revoked token being presented again (Story 11.2): revoke
    /// every other still-active token for the same user, since a revoked-token replay means the
    /// raw value leaked and every outstanding session for this user is suspect.
    Task RevokeAllActiveForUserAsync(Guid staffUserId, DateTime nowUtc, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
