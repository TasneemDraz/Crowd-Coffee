using CrowdCoffee.Domain.Staff;

namespace CrowdCoffee.Application.Auth;

public interface IStaffUserRepository
{
    Task<StaffUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<StaffUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
