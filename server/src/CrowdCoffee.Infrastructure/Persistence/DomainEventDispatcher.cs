using CrowdCoffee.Application.Common;
using CrowdCoffee.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace CrowdCoffee.Infrastructure.Persistence;

/// <summary>
/// Resolves IDomainEventHandler&lt;TEvent&gt; for each event's concrete runtime type via
/// reflection, since the event type is only known per-instance, not at compile time here.
/// </summary>
public class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = (IEnumerable<object>)serviceProvider.GetServices(handlerType) !;
            var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

            foreach (var handler in handlers)
            {
                await (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        }
    }
}
