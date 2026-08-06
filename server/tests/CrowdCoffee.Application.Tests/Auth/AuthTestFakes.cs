using CrowdCoffee.Application.Auth;
using CrowdCoffee.Domain.Staff;

namespace CrowdCoffee.Application.Tests.Auth;

/// Shared by LoginHandlerTests and RefreshHandlerTests — matches this test project's existing
/// no-mocking-library convention (hand-rolled fakes standing in for EF-backed implementations).
internal class FakeStaffUserRepository(params StaffUser[] users) : IStaffUserRepository
{
    public Task<StaffUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken) =>
        Task.FromResult(users.FirstOrDefault(u => u.Username == username));

    public Task<StaffUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(users.FirstOrDefault(u => u.Id == id));
}

/// Deliberately not real BCrypt — "hashed:{password}" is enough to exercise
/// correct-vs-wrong-password branching without pulling a real hashing algorithm into a unit test.
internal class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";
    public bool Verify(string password, string hash) => hash == $"hashed:{password}";
}

internal class FakeJwtTokenGenerator : IJwtTokenGenerator
{
    public AccessToken GenerateAccessToken(StaffUser user) =>
        new($"access-token-for-{user.Username}", DateTime.UtcNow.AddMinutes(15));
}

internal class FakeRefreshTokenStore : IRefreshTokenStore
{
    private readonly Dictionary<string, RefreshToken> _tokensByRawValue = [];

    public Task<(RefreshToken Token, string RawValue)> IssueAsync(Guid staffUserId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var rawValue = Guid.NewGuid().ToString();
        var token = new RefreshToken(Guid.NewGuid(), staffUserId, $"hash-of:{rawValue}", nowUtc.AddDays(7), nowUtc);
        _tokensByRawValue[rawValue] = token;
        return Task.FromResult((token, rawValue));
    }

    public Task<RefreshToken?> GetByRawValueAsync(string rawValue, CancellationToken cancellationToken) =>
        Task.FromResult(_tokensByRawValue.GetValueOrDefault(rawValue));

    public Task RevokeAllActiveForUserAsync(Guid staffUserId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        foreach (var token in _tokensByRawValue.Values.Where(t => t.StaffUserId == staffUserId && t.RevokedAtUtc is null))
        {
            token.Revoke(nowUtc);
        }

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
