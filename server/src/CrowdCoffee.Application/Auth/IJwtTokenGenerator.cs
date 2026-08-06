using CrowdCoffee.Domain.Staff;

namespace CrowdCoffee.Application.Auth;

public record AccessToken(string Token, DateTime ExpiresAtUtc);

public interface IJwtTokenGenerator
{
    AccessToken GenerateAccessToken(StaffUser user);
}
