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

    private static readonly OrderStatus[] ActiveKitchenStatuses =
        [OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Ready];

    public async Task<IReadOnlyList<Order>> GetActiveKitchenOrdersAsync(CancellationToken cancellationToken) =>
        await dbContext.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Modifiers)
            .Where(o => ActiveKitchenStatuses.Contains(o.Status))
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> SearchOrdersAsync(OrderSearchFilters filters, CancellationToken cancellationToken)
    {
        var query = dbContext.Orders.AsNoTracking().AsQueryable();

        if (filters.DateFrom is { } dateFrom)
        {
            query = query.Where(o => o.CreatedAt >= dateFrom.ToDateTime(TimeOnly.MinValue));
        }

        if (filters.DateTo is { } dateTo)
        {
            var exclusiveEnd = dateTo.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(o => o.CreatedAt < exclusiveEnd);
        }

        if (filters.Status is { } status)
        {
            query = query.Where(o => o.Status == status);
        }

        if (filters.Mode is { } mode)
        {
            query = query.Where(o => o.Mode == mode);
        }

        if (filters.PaymentMethod is { } paymentMethod)
        {
            query = query.Where(o => o.PaymentMethod == paymentMethod);
        }

        // No pagination UI exists yet (v1) — capped so an unfiltered search on a growing orders
        // table can't pull back an unbounded result set.
        return await query.OrderByDescending(o => o.CreatedAt).Take(200).ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdWithHistoryAsync(Guid orderId, CancellationToken cancellationToken) =>
        await dbContext.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Modifiers)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

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
