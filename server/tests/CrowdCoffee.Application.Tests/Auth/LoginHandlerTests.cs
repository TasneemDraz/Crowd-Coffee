using CrowdCoffee.Application.Auth;
using CrowdCoffee.Domain.Staff;

namespace CrowdCoffee.Application.Tests.Auth;

public class LoginHandlerTests
{
    private static readonly DateTime NowUtc = new(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);

    private static StaffUser ActiveManager => new(Guid.NewGuid(), "manager1", "hashed:Correct123!", StaffRole.Manager);

    [Fact]
    public async Task CorrectCredentials_ReturnsAccessAndRefreshTokens()
    {
        var user = ActiveManager;
        var handler = new LoginHandler(
            new FakeStaffUserRepository(user),
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenStore());

        var result = await handler.HandleAsync(new LoginRequest(user.Username, "Correct123!"), NowUtc, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Manager", result.Value.Role);
        Assert.Equal(user.Username, result.Value.Username);
        Assert.False(string.IsNullOrEmpty(result.Value.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.Value.RefreshToken));
    }

    [Fact]
    public async Task WrongPassword_ReturnsGenericUnauthorized()
    {
        var user = ActiveManager;
        var handler = new LoginHandler(
            new FakeStaffUserRepository(user),
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenStore());

        var result = await handler.HandleAsync(new LoginRequest(user.Username, "WrongPassword!"), NowUtc, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error!.Code);
    }

    [Fact]
    public async Task UnknownUsername_ReturnsSameGenericUnauthorized_AsWrongPassword()
    {
        var handler = new LoginHandler(
            new FakeStaffUserRepository(),
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenStore());

        var result = await handler.HandleAsync(new LoginRequest("nobody", "whatever"), NowUtc, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error!.Code);
    }

    [Fact]
    public async Task DeactivatedUser_IsRejected_EvenWithCorrectPassword()
    {
        var user = ActiveManager;
        user.Deactivate();

        var handler = new LoginHandler(
            new FakeStaffUserRepository(user),
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenStore());

        var result = await handler.HandleAsync(new LoginRequest(user.Username, "Correct123!"), NowUtc, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error!.Code);
    }
}
