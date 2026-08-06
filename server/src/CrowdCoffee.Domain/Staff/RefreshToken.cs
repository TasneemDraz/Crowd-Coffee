namespace CrowdCoffee.Domain.Staff;

/// <summary>
/// Backs Story 11.2's cookie-based refresh rotation. Only ever stores a hash of the raw token
/// value (hashing itself is an Infrastructure concern — this type just holds whatever the store
/// computed) so a DB leak alone can't be replayed as a working refresh token. RevokedAtUtc/
/// ReplacedByTokenId together support rotation-with-reuse-detection: a legitimate refresh revokes
/// the old token and links to its replacement, so a second attempt to use the same (now-revoked)
/// token is distinguishable from "just expired" and signals possible theft.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid StaffUserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    private RefreshToken()
    {
    }

    public RefreshToken(Guid id, Guid staffUserId, string tokenHash, DateTime expiresAtUtc, DateTime createdAtUtc)
    {
        Id = id;
        StaffUserId = staffUserId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public bool IsActive(DateTime nowUtc) => RevokedAtUtc is null && ExpiresAtUtc > nowUtc;

    public void Revoke(DateTime revokedAtUtc, Guid? replacedByTokenId = null)
    {
        RevokedAtUtc = revokedAtUtc;
        ReplacedByTokenId = replacedByTokenId;
    }
}
