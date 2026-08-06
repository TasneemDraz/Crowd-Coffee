using CrowdCoffee.Application.Common;
using CrowdCoffee.Application.Kitchen;
using CrowdCoffee.Application.Ordering;
using CrowdCoffee.Domain.Ordering;
using Microsoft.AspNetCore.SignalR;

namespace CrowdCoffee.Api.Hubs;

/// <summary>
/// Bridges Epic 3's domain-event pipeline to Epic 12's live kitchen board (Story 12.1, T12.1.2).
/// Order.TransitionTo raises OrderStatusChangedEvent (Domain, no I/O) on every successful
/// transition; this handler re-loads the order — same "don't trust whatever navigation
/// properties happened to be in memory at transition time" reasoning as the Epic 4 stock
/// handlers — and pushes the board-shaped DTO to every connected staff client. Lives in Api (not
/// Application/Infrastructure) because it's the only layer allowed to know about OrderHub/
/// IHubContext given this project's one-way layering (Api -> Application/Infrastructure ->
/// Domain).
/// </summary>
public class BroadcastOrderStatusChangedHandler(
    IHubContext<OrderHub> hubContext,
    IOrderRepository orderRepository) : IDomainEventHandler<OrderStatusChangedEvent>
{
    public async Task HandleAsync(OrderStatusChangedEvent domainEvent, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(domainEvent.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        await hubContext.Clients.All.SendAsync("orderStatusChanged", KitchenOrderDtoMapper.ToDto(order), cancellationToken);
    }
}
