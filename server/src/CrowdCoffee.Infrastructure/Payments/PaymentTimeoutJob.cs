using CrowdCoffee.Application.Ordering;
using CrowdCoffee.Domain.Ordering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CrowdCoffee.Infrastructure.Payments;

/// <summary>
/// Story 10.5: PendingPayment orders older than 30 minutes auto-cancel (§9). Deliberately doesn't
/// call StockReservationService — a PendingPayment order never had anything reserved (§8; see
/// Order.TransitionTo's comment on why cancelling from PendingPayment raises no domain event),
/// so there's nothing to release. Polls rather than using a precise per-order timer since 30
/// minutes of slop on a background sweep is immaterial to the business and far simpler than
/// scheduling one timer per pending order.
/// </summary>
public class PaymentTimeoutJob(IServiceScopeFactory scopeFactory, ILogger<PaymentTimeoutJob> logger) : BackgroundService
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CancelStaleOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payment timeout sweep failed; will retry next interval.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal on shutdown.
            }
        }
    }

    private async Task CancelStaleOrdersAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var cutoff = DateTime.UtcNow - Timeout;
        var staleOrders = await orderRepository.GetStalePendingPaymentOrdersAsync(cutoff, cancellationToken);
        if (staleOrders.Count == 0)
        {
            return;
        }

        foreach (var order in staleOrders)
        {
            var result = order.TransitionTo(OrderStatus.Cancelled, "payment-timeout-job", DateTime.UtcNow, "Payment not completed within 30 minutes.");
            if (result.IsFailure)
            {
                logger.LogWarning("Could not auto-cancel stale order {OrderId}: {ErrorCode}", order.Id, result.Error!.Code);
                continue;
            }

            logger.LogInformation("Auto-cancelled stale PendingPayment order {OrderId} ({OrderNumber}).", order.Id, order.OrderNumber);
        }

        await orderRepository.SaveChangesAsync(cancellationToken);
    }
}
