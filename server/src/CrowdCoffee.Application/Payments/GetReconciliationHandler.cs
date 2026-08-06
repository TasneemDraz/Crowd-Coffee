using CrowdCoffee.Application.Ordering;

namespace CrowdCoffee.Application.Payments;

public class GetReconciliationHandler(IPaymentEventStore paymentEventStore, IOrderRepository orderRepository)
{
    public async Task<IReadOnlyList<ReconciliationRowDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var events = await paymentEventStore.GetAllAsync(cancellationToken);
        var rows = new List<ReconciliationRowDto>(events.Count);

        foreach (var paymentEvent in events)
        {
            var order = await orderRepository.GetByIdAsync(paymentEvent.OrderId, cancellationToken);
            rows.Add(new ReconciliationRowDto(
                paymentEvent.OrderId,
                order?.OrderNumber,
                order?.Total,
                order?.Status.ToString(),
                paymentEvent.GatewayTransactionId,
                paymentEvent.AmountEgp,
                paymentEvent.IsSuccessful,
                paymentEvent.ReceivedAtUtc));
        }

        return rows;
    }
}
