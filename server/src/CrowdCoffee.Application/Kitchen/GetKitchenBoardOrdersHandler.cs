using CrowdCoffee.Application.Ordering;

namespace CrowdCoffee.Application.Kitchen;

public class GetKitchenBoardOrdersHandler(IOrderRepository orderRepository)
{
    public async Task<IReadOnlyList<KitchenOrderDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var orders = await orderRepository.GetActiveKitchenOrdersAsync(cancellationToken);
        return orders.Select(KitchenOrderDtoMapper.ToDto).ToList();
    }
}
