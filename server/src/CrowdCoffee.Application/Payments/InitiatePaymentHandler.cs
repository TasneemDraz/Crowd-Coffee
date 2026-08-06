using CrowdCoffee.Application.Ordering;
using CrowdCoffee.Domain.Common;
using CrowdCoffee.Domain.Ordering;

namespace CrowdCoffee.Application.Payments;

public record InitiatePaymentRequest(Guid OrderId);

/// <summary>
/// T10.4.1: the only inputs that matter are which order this is for — AmountEgp is read straight
/// off the persisted Order.Total, never accepted from the caller. Story 10.2's hosted-checkout
/// hand-off is "call this, then redirect the browser to the URL it returns."
/// </summary>
public class InitiatePaymentHandler(IOrderRepository orderRepository, IPaymentGateway paymentGateway)
{
    public async Task<Result<PaymentInitiationResult>> HandleAsync(InitiatePaymentRequest request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure<PaymentInitiationResult>(Error.NotFound("Payment.OrderNotFound", $"No order with id '{request.OrderId}'."));
        }

        if (order.PaymentMethod == PaymentMethod.Cash)
        {
            return Result.Failure<PaymentInitiationResult>(Error.Conflict("Payment.NotApplicable", "Cash orders do not go through the payment gateway."));
        }

        if (order.Status != OrderStatus.PendingPayment)
        {
            return Result.Failure<PaymentInitiationResult>(Error.Conflict(
                "Payment.OrderNotPendingPayment", $"Order is {order.Status}, not awaiting payment."));
        }

        return await paymentGateway.InitiateAsync(
            new PaymentInitiationRequest(order.Id, order.OrderNumber, order.Total, order.CustomerName, order.Phone),
            cancellationToken);
    }
}
