using CrowdCoffee.Application.Auth;
using CrowdCoffee.Domain.Staff;

namespace CrowdCoffee.Application.Tests.Auth;

public class RefreshHandlerTests
{
    private static readonly DateTime NowUtc = new(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);

    private static StaffUser Manager => new(Guid.NewGuid(), "manager1", "hashed:Correct123!", StaffRole.Manager);

    [Fact]
    public async Task ValidToken_RotatesToANewToken_AndRevokesTheOld()
    {
        var user = Manager;
        var store = new FakeRefreshTokenStore();
        var (originalToken, rawValue) = await store.IssueAsync(user.Id, NowUtc, CancellationToken.None);

        var handler = new RefreshHandler(store, new FakeStaffUserRepository(user), new FakeJwtTokenGenerator());

        var result = await handler.HandleAsync(rawValue, NowUtc.AddMinutes(1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(rawValue, result.Value.RefreshToken);
        Assert.NotNull(originalToken.RevokedAtUtc);
        Assert.Equal(user.Username, result.Value.Username);
    }

    [Fact]
    public async Task UnknownToken_IsRejected()
    {
        var handler = new RefreshHandler(new FakeRefreshTokenStore(), new FakeStaffUserRepository(), new FakeJwtTokenGenerator());

        var result = await handler.HandleAsync("never-issued", NowUtc, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidRefreshToken", result.Error!.Code);
    }

    [Fact]
    public async Task ExpiredToken_IsRejected()
    {
        var user = Manager;
        var store = new FakeRefreshTokenStore();
        var (_, rawValue) = await store.IssueAsync(user.Id, NowUtc, CancellationToken.None);

        var handler = new RefreshHandler(store, new FakeStaffUserRepository(user), new FakeJwtTokenGenerator());

        // Issued with a 7-day expiry (see FakeRefreshTokenStore) — 8 days later it's expired.
        var result = await handler.HandleAsync(rawValue, NowUtc.AddDays(8), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.RefreshTokenExpired", result.Error!.Code);
    }

    [Fact]
    public async Task RevokedTokenReused_IsRejected_AndRevokesEveryOtherActiveTokenForThatUser()
    {
        var user = Manager;
        var store = new FakeRefreshTokenStore();
        var (firstToken, firstRawValue) = await store.IssueAsync(user.Id, NowUtc, CancellationToken.None);
        var (secondToken, _) = await store.IssueAsync(user.Id, NowUtc, CancellationToken.None);

        var handler = new RefreshHandler(store, new FakeStaffUserRepository(user), new FakeJwtTokenGenerator());

        // Legitimately rotate the first token once...
        await handler.HandleAsync(firstRawValue, NowUtc.AddMinutes(1), CancellationToken.None);

        // ...then replay the now-revoked raw value, as a thief who captured it earlier would.
        var replayResult = await handler.HandleAsync(firstRawValue, NowUtc.AddMinutes(2), CancellationToken.None);

        Assert.True(replayResult.IsFailure);
        Assert.Equal("Auth.RefreshTokenReused", replayResult.Error!.Code);
        Assert.NotNull(firstToken.RevokedAtUtc);
        Assert.NotNull(secondToken.RevokedAtUtc); // the other outstanding session got swept too
    }
}
