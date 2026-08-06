using CrowdCoffee.Domain.Ordering;

namespace CrowdCoffee.Application.Kitchen;

/// Shared by GetKitchenBoardOrdersHandler's initial fetch and the Api-layer SignalR broadcast
/// handler — both need the exact same Order -> KitchenOrderDto shape.
public static class KitchenOrderDtoMapper
{
    public static KitchenOrderDto ToDto(Order order) => new(
        order.Id,
        order.OrderNumber,
        order.Mode.ToString(),
        order.Address,
        order.Latitude,
        order.Longitude,
        order.CustomerName,
        order.Phone,
        order.Status.ToString(),
        order.CreatedAt,
        order.ConfirmedAt,
        order.Notes,
        order.Items
            .Select(i => new KitchenOrderLineDto(i.ProductName, i.Quantity, i.Modifiers.Select(m => m.ModifierName).ToList()))
            .ToList());
}
