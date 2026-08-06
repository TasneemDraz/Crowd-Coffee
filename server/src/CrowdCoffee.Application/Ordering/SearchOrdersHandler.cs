namespace CrowdCoffee.Application.Ordering;

public class SearchOrdersHandler(IOrderRepository orderRepository)
{
    public async Task<IReadOnlyList<AdminOrderSummaryDto>> HandleAsync(OrderSearchFilters filters, CancellationToken cancellationToken)
    {
        var orders = await orderRepository.SearchOrdersAsync(filters, cancellationToken);
        return orders
            .Select(o => new AdminOrderSummaryDto(
                o.Id, o.OrderNumber, o.CustomerName, o.Mode.ToString(), o.PaymentMethod.ToString(),
                o.Status.ToString(), o.Total, o.CreatedAt))
            .ToList();
    }
}
