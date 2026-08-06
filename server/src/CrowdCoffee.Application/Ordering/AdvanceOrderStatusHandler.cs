using CrowdCoffee.Domain.Common;
using CrowdCoffee.Domain.Ordering;

namespace CrowdCoffee.Application.Ordering;

/// <summary>
/// Epic 12's one-tap kitchen-board advance. Deliberately doesn't accept a target status from the
/// caller — the board only ever offers "move to the next column," so the server computes what
/// that means (branching at Ready per the order's Mode, same rule Order.TransitionTo itself
/// guards) rather than trusting a client-supplied status.
/// </summary>
public class AdvanceOrderStatusHandler(IOrderRepository orderRepository)
{
    public async Task<Result<OrderDto>> HandleAsync(Guid orderId, string changedBy, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure<OrderDto>(Error.NotFound("Order.NotFound", $"No order with id '{orderId}'."));
        }

        var nextStatus = order.Status switch
        {
            OrderStatus.Confirmed => OrderStatus.Preparing,
            OrderStatus.Preparing => OrderStatus.Ready,
            OrderStatus.Ready => order.Mode == OrderMode.Delivery ? OrderStatus.OutForDelivery : OrderStatus.PickedUp,
            _ => (OrderStatus?)null
        };

        if (nextStatus is null)
        {
            return Result.Failure<OrderDto>(Error.Conflict(
                "Order.NoNextKitchenStatus", $"Order is {order.Status}, which has no kitchen-board next step."));
        }

        var transitionResult = order.TransitionTo(nextStatus.Value, changedBy, nowUtc);
        if (transitionResult.IsFailure)
        {
            return Result.Failure<OrderDto>(transitionResult.Error!);
        }

        await orderRepository.SaveChangesAsync(cancellationToken);
        return Result.Success(OrderDtoMapper.ToDto(order));
    }
}
