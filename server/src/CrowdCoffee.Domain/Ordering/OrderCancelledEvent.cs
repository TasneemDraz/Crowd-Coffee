using CrowdCoffee.Domain.Common;

namespace CrowdCoffee.Domain.Ordering;

/// <summary>
/// Carries only the OrderId, deliberately — not the Order's in-memory Items. A handler (Epic 4:
/// release stock reservations) should load the order's items itself via a repository rather than
/// trusting whatever the aggregate happened to have loaded at transition time, which may be
/// incomplete if the caller didn't Include() navigation properties before calling TransitionTo.
/// </summary>
public sealed record OrderCancelledEvent(Guid OrderId) : IDomainEvent;
