namespace CrowdCoffee.Infrastructure.Persistence;

/// <summary>
/// One row per gateway webhook actually processed (Story 10.3 idempotency, Story 10.6
/// reconciliation). Infrastructure-only like OrderNumberCounter — Domain has no concept of a
/// "gateway transaction", only PaymentMethod/PaymentStatus on Order.
/// </summary>
public class PaymentEvent
{
    public Guid Id { get; set; }

    public string GatewayTransactionId { get; set; } = string.Empty;

    public Guid OrderId { get; set; }

    public decimal AmountEgp { get; set; }

    public bool IsSuccessful { get; set; }

    public DateTime ReceivedAtUtc { get; set; }
}
