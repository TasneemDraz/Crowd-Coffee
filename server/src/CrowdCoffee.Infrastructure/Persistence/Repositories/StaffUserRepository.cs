using CrowdCoffee.Application.Auth;
using CrowdCoffee.Domain.Staff;
using Microsoft.EntityFrameworkCore;

namespace CrowdCoffee.Infrastructure.Persistence.Repositories;

public class StaffUserRepository(CrowdCoffeeDbContext dbContext) : IStaffUserRepository
{
    public Task<StaffUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken) =>
        dbContext.StaffUsers.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public Task<StaffUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.StaffUsers.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
}
