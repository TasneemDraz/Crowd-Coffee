using CrowdCoffee.Domain.Ordering;

namespace CrowdCoffee.Application.Notifications;

/// <summary>
/// Story 15.2: customer-facing notifications, deliberately narrow — one method per event this
/// project actually needs to tell a customer about (§14.5's open question on customer tracking
/// is still unresolved, so this stays notification-only, not a status API). A no-op/logging
/// implementation is acceptable for v1 (AC) — the interface is what lets a real
/// SMS/WhatsApp/push adapter drop in later (Infrastructure) without touching call sites.
/// </summary>
public interface INotificationService
{
    Task NotifyOrderConfirmedAsync(Order order, CancellationToken cancellationToken);

    Task NotifyOrderReadyAsync(Order order, CancellationToken cancellationToken);

    Task NotifyOrderOutForDeliveryAsync(Order order, CancellationToken cancellationToken);
}
