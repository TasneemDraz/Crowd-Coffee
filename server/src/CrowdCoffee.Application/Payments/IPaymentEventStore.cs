namespace CrowdCoffee.Application.Payments;

public record PaymentEventRecord(Guid Id, string GatewayTransactionId, Guid OrderId, decimal AmountEgp, bool IsSuccessful, DateTime ReceivedAtUtc);

/// <summary>
/// Backs both webhook idempotency (Story 10.3: "gateways retry") and the reconciliation view
/// (Story 10.6: gateway-confirmed payments vs. orders) — the same append-only record of what the
/// gateway told us serves both.
/// </summary>
public interface IPaymentEventStore
{
    Task<bool> HasProcessedAsync(string gatewayTransactionId, CancellationToken cancellationToken);

    void Add(PaymentEventRecord record);

    Task<IReadOnlyList<PaymentEventRecord>> GetAllAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
