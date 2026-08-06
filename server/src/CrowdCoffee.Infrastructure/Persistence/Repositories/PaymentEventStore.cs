using CrowdCoffee.Application.Payments;
using Microsoft.EntityFrameworkCore;

namespace CrowdCoffee.Infrastructure.Persistence.Repositories;

public class PaymentEventStore(CrowdCoffeeDbContext dbContext) : IPaymentEventStore
{
    public Task<bool> HasProcessedAsync(string gatewayTransactionId, CancellationToken cancellationToken) =>
        dbContext.PaymentEvents.AnyAsync(e => e.GatewayTransactionId == gatewayTransactionId, cancellationToken);

    public void Add(PaymentEventRecord record) => dbContext.PaymentEvents.Add(new PaymentEvent
    {
        Id = record.Id,
        GatewayTransactionId = record.GatewayTransactionId,
        OrderId = record.OrderId,
        AmountEgp = record.AmountEgp,
        IsSuccessful = record.IsSuccessful,
        ReceivedAtUtc = record.ReceivedAtUtc
    });

    public async Task<IReadOnlyList<PaymentEventRecord>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.PaymentEvents.AsNoTracking()
            .OrderByDescending(e => e.ReceivedAtUtc)
            .Select(e => new PaymentEventRecord(e.Id, e.GatewayTransactionId, e.OrderId, e.AmountEgp, e.IsSuccessful, e.ReceivedAtUtc))
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
