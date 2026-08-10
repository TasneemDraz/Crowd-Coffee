using CrowdCoffee.Application.Common;
using CrowdCoffee.Application.Ordering;
using CrowdCoffee.Domain.Ordering;

namespace CrowdCoffee.Application.Notifications;

/// <summary>
/// Story 15.2 (T15.2.2): reacts to the same OrderStatusChangedEvent Epic 12's kitchen board
/// broadcast handler uses (Order.TransitionTo raises it on every successful transition) rather
/// than adding new call sites inside Order/CreateOrderHandler/AdvanceOrderStatusHandler/the
/// webhook handler — those already all funnel through TransitionTo, so this one handler covers
/// Confirmed (cash auto-confirm and paid card/wallet), Ready (kitchen board advance), and
/// OutForDelivery (kitchen board advance) regardless of which call site triggered the
/// transition. Re-loads the order for the same "don't trust in-memory navigation" reasoning as
/// the Epic 4/12 handlers.
/// </summary>
public class SendCustomerNotificationOnOrderStatusChangedHandler(
    INotificationService notificationService,
    IOrderRepository orderRepository) : IDomainEventHandler<OrderStatusChangedEvent>
{
    public async Task HandleAsync(OrderStatusChangedEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.NewStatus is not (OrderStatus.Confirmed or OrderStatus.Ready or OrderStatus.OutForDelivery))
        {
            return;
        }

        var order = await orderRepository.GetByIdAsync(domainEvent.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        switch (domainEvent.NewStatus)
        {
            case OrderStatus.Confirmed:
                await notificationService.NotifyOrderConfirmedAsync(order, cancellationToken);
                break;
            case OrderStatus.Ready:
                await notificationService.NotifyOrderReadyAsync(order, cancellationToken);
                break;
            case OrderStatus.OutForDelivery:
                await notificationService.NotifyOrderOutForDeliveryAsync(order, cancellationToken);
                break;
        }
    }
}
