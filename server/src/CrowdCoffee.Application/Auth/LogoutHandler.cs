namespace CrowdCoffee.Application.Auth;

public class LogoutHandler(IRefreshTokenStore refreshTokenStore)
{
    public async Task HandleAsync(string rawRefreshToken, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var token = await refreshTokenStore.GetByRawValueAsync(rawRefreshToken, cancellationToken);
        if (token is not null && token.RevokedAtUtc is null)
        {
            token.Revoke(nowUtc);
            await refreshTokenStore.SaveChangesAsync(cancellationToken);
        }
    }
}
