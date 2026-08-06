using CrowdCoffee.Domain.Common;

namespace CrowdCoffee.Application.Ordering;

/// Story 13.4's order-detail view: full status-history timeline plus modifiers spelled out per
/// line — distinct from GetOrderHandler/OrderDto (Epic 9/10, used by the checkout-return poll),
/// which neither needs nor loads status history.
public class GetOrderDetailHandler(IOrderRepository orderRepository)
{
    public async Task<Result<AdminOrderDetailDto>> HandleAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdWithHistoryAsync(orderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure<AdminOrderDetailDto>(Error.NotFound("Order.NotFound", $"No order with id '{orderId}'."));
        }

        var lines = order.Items
            .Select(i => new AdminOrderLineDto(
                i.ProductName, i.Quantity, i.UnitPrice, i.LineTotal,
                i.Modifiers.Select(m => m.ModifierName).ToList()))
            .ToList();

        var history = order.StatusHistory
            .OrderBy(h => h.ChangedAt)
            .Select(h => new OrderStatusHistoryDto(h.FromStatus.ToString(), h.ToStatus.ToString(), h.ChangedBy, h.ChangedAt, h.Reason))
            .ToList();

        return Result.Success(new AdminOrderDetailDto(
            order.Id, order.OrderNumber, order.CustomerName, order.Phone, order.Mode.ToString(),
            order.Address, order.Latitude, order.Longitude, order.Subtotal, order.DeliveryFee, order.Total,
            order.PaymentMethod.ToString(), order.PaymentStatus.ToString(), order.Status.ToString(),
            order.CreatedAt, order.ConfirmedAt, order.CompletedAt, order.Notes, lines, history));
    }
}
