using CrowdCoffee.Domain.Common;

namespace CrowdCoffee.Domain.Ordering;

/// <summary>
/// Raised on every successful TransitionTo call, unlike OrderCancelledEvent/OrderCompletedEvent
/// which only fire on specific edges — Epic 12's kitchen board needs to know about every step
/// (Confirmed, Preparing, Ready, ...), not just cancellation/completion. Carries only the new
/// status, same "handler re-loads the order itself" reasoning as the other Order events.
/// </summary>
public sealed record OrderStatusChangedEvent(Guid OrderId, OrderStatus NewStatus) : IDomainEvent;
