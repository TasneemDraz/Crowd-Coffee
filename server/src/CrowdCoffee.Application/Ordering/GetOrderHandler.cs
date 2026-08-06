using CrowdCoffee.Domain.Common;

namespace CrowdCoffee.Application.Ordering;

/// <summary>
/// Backs the checkout return page (Story 10.2, T10.2.2): after the gateway redirects the
/// customer's browser back to the SPA with no client-side state left (a full page reload through
/// a third-party domain), the SPA polls this by order id to learn the real, server-confirmed
/// outcome — never trusting whatever query parameters the redirect itself carried.
/// </summary>
public class GetOrderHandler(IOrderRepository orderRepository)
{
    public async Task<Result<OrderDto>> HandleAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
        return order is null
            ? Result.Failure<OrderDto>(Error.NotFound("Order.NotFound", $"No order with id '{orderId}'."))
            : Result.Success(OrderDtoMapper.ToDto(order));
    }
}
