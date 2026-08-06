using CrowdCoffee.Application.Ordering;
using CrowdCoffee.Domain.Ordering;
using Microsoft.EntityFrameworkCore;

namespace CrowdCoffee.Infrastructure.Persistence.Repositories;

public class OrderRepository(CrowdCoffeeDbContext dbContext) : IOrderRepository
{
    public async Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        await dbContext.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Modifiers)
            .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        await dbContext.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Modifiers)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetStalePendingPaymentOrdersAsync(DateTime olderThanUtc, CancellationToken cancellationToken) =>
        await dbContext.Orders
            .Where(o => o.Status == OrderStatus.PendingPayment && o.CreatedAt < olderThanUtc)
            .ToListAsync(cancellationToken);

    public void Add(Order order) => dbContext.Orders.Add(order);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsIdempotencyKeyViolation(ex))
        {
            throw new OrderIdempotencyConflictException();
        }
    }

    private static bool IsIdempotencyKeyViolation(DbUpdateException ex) =>
        ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2627 or 2601 } sqlEx &&
        sqlEx.Message.Contains("IdempotencyKey", StringComparison.OrdinalIgnoreCase);
}
