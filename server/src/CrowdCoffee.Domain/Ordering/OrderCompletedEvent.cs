using CrowdCoffee.Domain.Common;

namespace CrowdCoffee.Domain.Ordering;

/// <summary>
/// Raised on the terminal Delivered/PickedUp transition (§8: reservation converts to a Sale
/// movement on completion). Carries only the OrderId for the same reason OrderCancelledEvent
/// does — the handler (Epic 4: fulfill stock reservation) re-loads the order's items itself
/// rather than trusting whatever navigation properties happened to be loaded at transition time.
/// </summary>
public sealed record OrderCompletedEvent(Guid OrderId) : IDomainEvent;
