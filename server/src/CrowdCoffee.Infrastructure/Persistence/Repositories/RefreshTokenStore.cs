using System.Security.Cryptography;
using System.Text;
using CrowdCoffee.Application.Auth;
using CrowdCoffee.Domain.Staff;
using CrowdCoffee.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CrowdCoffee.Infrastructure.Persistence.Repositories;

public class RefreshTokenStore(CrowdCoffeeDbContext dbContext, IOptions<JwtOptions> jwtOptions) : IRefreshTokenStore
{
    public Task<(RefreshToken Token, string RawValue)> IssueAsync(Guid staffUserId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var rawValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var token = new RefreshToken(
            Guid.NewGuid(), staffUserId, Hash(rawValue),
            nowUtc.AddDays(jwtOptions.Value.RefreshTokenDays), nowUtc);

        dbContext.RefreshTokens.Add(token);
        return Task.FromResult((token, rawValue));
    }

    public Task<RefreshToken?> GetByRawValueAsync(string rawValue, CancellationToken cancellationToken)
    {
        var hash = Hash(rawValue);
        return dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
    }

    public async Task RevokeAllActiveForUserAsync(Guid staffUserId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(t => t.StaffUserId == staffUserId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(nowUtc);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    private static string Hash(string rawValue) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawValue)));
}
