using CrowdCoffee.Domain.Common;

namespace CrowdCoffee.Application.Auth;

public class LoginHandler(
    IStaffUserRepository staffUserRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IRefreshTokenStore refreshTokenStore)
{
    public async Task<Result<AuthResult>> HandleAsync(LoginRequest request, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var user = await staffUserRepository.GetByUsernameAsync(request.Username, cancellationToken);

        // Same generic error for "no such user" and "wrong password" — a distinct message would
        // let an attacker enumerate valid staff usernames.
        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result.Failure<AuthResult>(Error.Unauthorized("Auth.InvalidCredentials", "Invalid username or password."));
        }

        var access = jwtTokenGenerator.GenerateAccessToken(user);
        var (refreshToken, rawRefreshToken) = await refreshTokenStore.IssueAsync(user.Id, nowUtc, cancellationToken);
        await refreshTokenStore.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthResult(
            access.Token, access.ExpiresAtUtc,
            rawRefreshToken, refreshToken.ExpiresAtUtc,
            user.Username, user.Role.ToString()));
    }
}
