using CrowdCoffee.Domain.Common;

namespace CrowdCoffee.Application.Auth;

public class RefreshHandler(
    IRefreshTokenStore refreshTokenStore,
    IStaffUserRepository staffUserRepository,
    IJwtTokenGenerator jwtTokenGenerator)
{
    public async Task<Result<AuthResult>> HandleAsync(string rawRefreshToken, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var token = await refreshTokenStore.GetByRawValueAsync(rawRefreshToken, cancellationToken);
        if (token is null)
        {
            return Result.Failure<AuthResult>(Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid."));
        }

        if (token.RevokedAtUtc is not null)
        {
            // A revoked token being presented again means the raw value leaked (rotation already
            // moved this session to a different token) — treat every other active session for
            // this user as compromised too, not just this one request.
            await refreshTokenStore.RevokeAllActiveForUserAsync(token.StaffUserId, nowUtc, cancellationToken);
            await refreshTokenStore.SaveChangesAsync(cancellationToken);
            return Result.Failure<AuthResult>(Error.Unauthorized("Auth.RefreshTokenReused", "Refresh token has already been used."));
        }

        if (!token.IsActive(nowUtc))
        {
            return Result.Failure<AuthResult>(Error.Unauthorized("Auth.RefreshTokenExpired", "Refresh token has expired."));
        }

        var user = await staffUserRepository.GetByIdAsync(token.StaffUserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthResult>(Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid."));
        }

        var (newToken, rawNewToken) = await refreshTokenStore.IssueAsync(user.Id, nowUtc, cancellationToken);
        token.Revoke(nowUtc, newToken.Id);
        await refreshTokenStore.SaveChangesAsync(cancellationToken);

        var access = jwtTokenGenerator.GenerateAccessToken(user);
        return Result.Success(new AuthResult(
            access.Token, access.ExpiresAtUtc,
            rawNewToken, newToken.ExpiresAtUtc,
            user.Username, user.Role.ToString()));
    }
}
