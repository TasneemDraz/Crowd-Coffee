using System.Net.Http.Json;
using System.Text.Json;
using CrowdCoffee.Application.Payments;
using CrowdCoffee.Domain.Common;
using CrowdCoffee.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrowdCoffee.Infrastructure.Payments;

/// <summary>
/// Story 10.1's Paymob adapter: auth-token → register-order → payment-key → iframe redirect
/// (Paymob's "classic" integration, chosen over their newer unified-intention API since it's the
/// one with the widest documented card + Vodafone Cash + InstaPay coverage under one integration
/// id, matching the Context &amp; Decisions note in docs/EPIC_BREAKDOWN.md). No gateway-specific
/// type crosses out of this class — IPaymentGateway's request/result records are all callers ever
/// see.
/// </summary>
public class PaymobGateway(HttpClient httpClient, IOptions<PaymobOptions> options, ILogger<PaymobGateway> logger) : IPaymentGateway
{
    private readonly PaymobOptions _options = options.Value;

    public async Task<Result<PaymentInitiationResult>> InitiateAsync(PaymentInitiationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.IframeId) || _options.IntegrationId == 0)
        {
            return Result.Failure<PaymentInitiationResult>(Error.Conflict(
                "Payment.NotConfigured",
                "Paymob is not configured (missing API key, integration id, or iframe id). Set Paymob:* via environment variables."));
        }

        // T10.4.1: the caller (CreatePaymentIntentHandler) reads AmountEgp from the persisted
        // Order.Total — nothing here accepts an amount from the original checkout request.
        var amountCents = (long)Math.Round(request.AmountEgp * 100, MidpointRounding.AwayFromZero);

        try
        {
            var authToken = await GetAuthTokenAsync(cancellationToken);

            var order = await httpClient.PostAsJsonAsync(
                "/api/ecommerce/orders",
                new RegisterOrderRequest(authToken, DeliveryNeeded: false, amountCents, "EGP", request.OrderId.ToString(), Items: []),
                cancellationToken);
            order.EnsureSuccessStatusCode();
            var orderBody = await order.Content.ReadFromJsonAsync<RegisterOrderResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Paymob order registration returned an empty body.");

            var nameParts = request.CustomerName.Trim().Split(' ', 2);
            var billingData = new BillingData(
                FirstName: nameParts[0],
                LastName: nameParts.Length > 1 ? nameParts[1] : "NA",
                PhoneNumber: request.CustomerPhone,
                Email: "customer@crowdcoffee.local", // not collected at checkout (§9.1 form has no email field)
                Apartment: "NA", Floor: "NA", Street: "NA", Building: "NA",
                City: "Port Said", Country: "EG", State: "NA");

            var paymentKey = await httpClient.PostAsJsonAsync(
                "/api/acceptance/payment_keys",
                new PaymentKeyRequest(authToken, amountCents, "EGP", orderBody.Id, billingData, _options.IntegrationId),
                cancellationToken);
            paymentKey.EnsureSuccessStatusCode();
            var paymentKeyBody = await paymentKey.Content.ReadFromJsonAsync<PaymentKeyResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Paymob payment-key request returned an empty body.");

            var redirectUrl = $"{_options.ApiBaseUrl}/api/acceptance/iframes/{_options.IframeId}?payment_token={paymentKeyBody.Token}";
            return Result.Success(new PaymentInitiationResult(redirectUrl));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        {
            logger.LogError(ex, "Paymob payment initiation failed for order {OrderId}.", request.OrderId);
            return Result.Failure<PaymentInitiationResult>(Error.Conflict("Payment.GatewayError", "Could not start payment with the payment provider. Please try again."));
        }
    }

    public Result<VerifiedPaymentWebhook> VerifyAndParseWebhook(string rawBody, string? signature)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(rawBody);
        }
        catch (JsonException)
        {
            return Result.Failure<VerifiedPaymentWebhook>(Error.Validation("Payment.MalformedWebhook", "Webhook body is not valid JSON."));
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("obj", out var obj))
            {
                return Result.Failure<VerifiedPaymentWebhook>(Error.Validation("Payment.MalformedWebhook", "Webhook body is missing 'obj'."));
            }

            if (!PaymobHmac.Verify(obj, _options.HmacSecret, signature))
            {
                return Result.Failure<VerifiedPaymentWebhook>(Error.Unauthorized("Payment.InvalidSignature", "Webhook signature verification failed."));
            }

            var transaction = obj.Deserialize<WebhookTransactionObj>()
                ?? throw new InvalidOperationException("HMAC-verified webhook body failed to deserialize — this should be unreachable.");

            if (transaction.Order?.MerchantOrderId is not { } merchantOrderId || !Guid.TryParse(merchantOrderId, out var orderId))
            {
                return Result.Failure<VerifiedPaymentWebhook>(Error.Validation("Payment.UnknownOrder", "Webhook did not carry a recognizable merchant_order_id."));
            }

            return Result.Success(new VerifiedPaymentWebhook(
                transaction.Id.ToString(),
                orderId,
                transaction.Success,
                transaction.AmountCents / 100m));
        }
    }

    private async Task<string> GetAuthTokenAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("/api/auth/tokens", new AuthTokenRequest(_options.ApiKey), cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthTokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Paymob auth-token request returned an empty body.");
        return body.Token;
    }
}
