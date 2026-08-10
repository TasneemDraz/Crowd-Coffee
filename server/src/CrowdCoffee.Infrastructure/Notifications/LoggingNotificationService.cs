using CrowdCoffee.Application.Notifications;
using CrowdCoffee.Domain.Ordering;
using Microsoft.Extensions.Logging;

namespace CrowdCoffee.Infrastructure.Notifications;

/// <summary>
/// Story 15.2's AC-sanctioned v1 implementation: logs what would have gone out (to which phone
/// number, for which order) instead of actually sending SMS/WhatsApp/push — no notification
/// provider is contracted for this project yet (same "no live creds" situation as Epic 10's
/// Paymob adapter before it). Swappable behind INotificationService without touching the
/// Application-layer call site once a real provider exists.
/// </summary>
public class LoggingNotificationService(ILogger<LoggingNotificationService> logger) : INotificationService
{
    public Task NotifyOrderConfirmedAsync(Order order, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Notification (no-op): order {OrderNumber} confirmed — would notify {Phone}.",
            order.OrderNumber, order.Phone);
        return Task.CompletedTask;
    }

    public Task NotifyOrderReadyAsync(Order order, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Notification (no-op): order {OrderNumber} ready — would notify {Phone}.",
            order.OrderNumber, order.Phone);
        return Task.CompletedTask;
    }

    public Task NotifyOrderOutForDeliveryAsync(Order order, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Notification (no-op): order {OrderNumber} out for delivery — would notify {Phone}.",
            order.OrderNumber, order.Phone);
        return Task.CompletedTask;
    }
}
