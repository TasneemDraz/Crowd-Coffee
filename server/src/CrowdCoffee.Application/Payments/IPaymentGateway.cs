using CrowdCoffee.Domain.Common;

namespace CrowdCoffee.Application.Payments;

/// <summary>
/// §9: card/wallet payment without touching raw card data. Everything gateway-specific (Paymob's
/// auth-token/order/payment-key dance, its HMAC field-concatenation scheme) stays inside the
/// Infrastructure adapter — Application only ever sees these generic shapes, so swapping in Fawry
/// later means writing a new IPaymentGateway implementation, not touching any caller.
/// </summary>
public interface IPaymentGateway
{
    /// T10.4.1: AmountEgp must come from the caller reading Order.Total from the DB — this
    /// interface has no way to accept a client-supplied amount, by design.
    Task<Result<PaymentInitiationResult>> InitiateAsync(PaymentInitiationRequest request, CancellationToken cancellationToken);

    /// Synchronous and pure (no I/O): verifies the HMAC over the raw callback body/signature and,
    /// only if valid, parses it into a gateway-agnostic result. Returns Result.Failure with
    /// ErrorType.Unauthorized on a missing/mismatched signature — callers must reject those
    /// outright (§9: "reject unsigned or mismatched callbacks"), never partially trust them.
    Result<VerifiedPaymentWebhook> VerifyAndParseWebhook(string rawBody, string? signature);
}

public record PaymentInitiationRequest(
    Guid OrderId,
    string OrderNumber,
    decimal AmountEgp,
    string CustomerName,
    string CustomerPhone);

public record PaymentInitiationResult(string RedirectUrl);

/// GatewayTransactionId backs webhook idempotency (§9: "gateways retry") and Epic 10.6's
/// reconciliation view — it's the one field every repeat delivery of the same event shares.
public record VerifiedPaymentWebhook(
    string GatewayTransactionId,
    Guid OrderId,
    bool IsSuccessful,
    decimal AmountEgp);
