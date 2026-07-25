using CrowdCoffee.Application.Common;
using CrowdCoffee.Domain.Ordering;
using Microsoft.Extensions.Logging;

namespace CrowdCoffee.Application.Ordering;

/// <summary>
/// Proves the domain event pipeline end-to-end. Epic 4 adds the handler that actually releases
/// stock reservations on cancellation — this one stays regardless, since logging a cancellation
/// is useful in its own right.
/// </summary>
public class LoggingOrderCancelledHandler(ILogger<LoggingOrderCancelledHandler> logger) : IDomainEventHandler<OrderCancelledEvent>
{
    public Task HandleAsync(OrderCancelledEvent domainEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation("Order {OrderId} was cancelled.", domainEvent.OrderId);
        return Task.CompletedTask;
    }
}
