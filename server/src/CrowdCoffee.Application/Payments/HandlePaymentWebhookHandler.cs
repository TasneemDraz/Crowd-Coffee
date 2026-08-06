using CrowdCoffee.Application.Common;
using CrowdCoffee.Application.Inventory;
using CrowdCoffee.Application.Ordering;
using CrowdCoffee.Domain.Common;
using CrowdCoffee.Domain.Ordering;
using Microsoft.Extensions.Logging;

namespace CrowdCoffee.Application.Payments;

/// <summary>
/// Story 10.3. §9's three requirements land here: reject unsigned/mismatched callbacks (delegated
/// to IPaymentGateway.VerifyAndParseWebhook, which fails the whole request before this handler's
/// business logic ever runs), idempotent handling of repeats (IPaymentEventStore dedupe), and
/// wiring a confirmed payment to the state machine (Confirmed) which is what actually triggers
/// stock reservation — same "cash orders auto-confirm at checkout" mechanism Story 9.2 uses,
/// just triggered by the gateway instead of by CreateOrderHandler.
/// </summary>
public class HandlePaymentWebhookHandler(
    IPaymentGateway paymentGateway,
    IPaymentEventStore paymentEventStore,
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    StockReservationService stockReservationService,
    ILogger<HandlePaymentWebhookHandler> logger)
{
    public async Task<Result> HandleAsync(string rawBody, string? signature, CancellationToken cancellationToken)
    {
        var parseResult = paymentGateway.VerifyAndParseWebhook(rawBody, signature);
        if (parseResult.IsFailure)
        {
            logger.LogWarning("Rejected payment webhook: {ErrorCode} {ErrorMessage}", parseResult.Error!.Code, parseResult.Error.Message);
            return Result.Failure(parseResult.Error);
        }

        var webhook = parseResult.Value;

        if (await paymentEventStore.HasProcessedAsync(webhook.GatewayTransactionId, cancellationToken))
        {
            return Result.Success(); // §9: gateways retry — already handled, no-op.
        }

        var order = await orderRepository.GetByIdAsync(webhook.OrderId, cancellationToken);
        if (order is null)
        {
            // Nothing further we can do for an order id that doesn't exist — record the event
            // (so a retry of this exact transaction id short-circuits above) and return success
            // so the gateway stops retrying a call that will never succeed differently.
            logger.LogWarning("Payment webhook for unknown order {OrderId} (gateway tx {TxId}).", webhook.OrderId, webhook.GatewayTransactionId);
            paymentEventStore.Add(new PaymentEventRecord(Guid.NewGuid(), webhook.GatewayTransactionId, webhook.OrderId, webhook.AmountEgp, webhook.IsSuccessful, DateTime.UtcNow));
            await paymentEventStore.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        paymentEventStore.Add(new PaymentEventRecord(Guid.NewGuid(), webhook.GatewayTransactionId, order.Id, webhook.AmountEgp, webhook.IsSuccessful, DateTime.UtcNow));

        if (order.Status != OrderStatus.PendingPayment)
        {
            // Already resolved by an earlier event (or the order timed out and was cancelled
            // before this delivery arrived) — keep the event on record for reconciliation but
            // don't re-transition an order that's already moved on.
            await paymentEventStore.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result.Success();
        }

        if (webhook.IsSuccessful)
        {
            order.MarkPaymentPaid();
            var confirmResult = order.TransitionTo(OrderStatus.Confirmed, "paymob-webhook", DateTime.UtcNow, "Payment confirmed via webhook.");
            if (confirmResult.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure(confirmResult.Error!);
            }

            await orderRepository.SaveChangesAsync(cancellationToken);

            var reserveResult = await stockReservationService.ReserveForOrderAsync(order.Id, cancellationToken);
            if (reserveResult.IsFailure)
            {
                // The charge already succeeded — there is no "reject the payment" undo available
                // from here. Leave the order Confirmed/Paid (visible to staff on the kitchen
                // board) and log loudly for manual handling rather than failing the webhook,
                // which would just make the gateway retry the same doomed reservation forever.
                logger.LogError(
                    "Order {OrderId} paid successfully but stock reservation failed: {ErrorCode} {ErrorMessage}. Manual intervention required (oversold item).",
                    order.Id, reserveResult.Error!.Code, reserveResult.Error.Message);
            }
        }
        else
        {
            order.MarkPaymentFailed();
            var cancelResult = order.TransitionTo(OrderStatus.Cancelled, "paymob-webhook", DateTime.UtcNow, "Payment failed.");
            if (cancelResult.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure(cancelResult.Error!);
            }

            await orderRepository.SaveChangesAsync(cancellationToken);
        }

        await paymentEventStore.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
