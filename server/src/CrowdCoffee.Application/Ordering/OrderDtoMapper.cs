using CrowdCoffee.Domain.Ordering;

namespace CrowdCoffee.Application.Ordering;

/// Shared by CreateOrderHandler and GetOrderHandler — both need the exact same Order → OrderDto
/// shape (the checkout return page polls GetOrderHandler for the same DTO CreateOrderHandler
/// already returned synchronously for cash orders).
public static class OrderDtoMapper
{
    public static OrderDto ToDto(Order order) => new(
        order.Id,
        order.OrderNumber,
        order.CustomerName,
        order.Phone,
        order.Mode.ToString(),
        order.Address,
        order.Latitude,
        order.Longitude,
        order.Subtotal,
        order.DeliveryFee,
        order.Total,
        order.PaymentMethod.ToString(),
        order.PaymentStatus.ToString(),
        order.Status.ToString(),
        order.CreatedAt,
        order.Items.Select(i => new OrderLineDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.LineTotal)).ToList());
}
