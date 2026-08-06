using System.Text.Json.Serialization;

namespace CrowdCoffee.Infrastructure.Payments;

/// <summary>
/// Wire shapes for Paymob's classic (auth-token → order → payment-key → iframe) flow, per
/// https://developers.paymob.com. Deliberately internal to this namespace/assembly — nothing
/// outside PaymobGateway ever sees these; IPaymentGateway's generic DTOs are what the rest of the
/// app depends on (Story 10.1's "no gateway-specific types leak outside Infrastructure").
/// </summary>
internal record AuthTokenRequest([property: JsonPropertyName("api_key")] string ApiKey);

internal record AuthTokenResponse([property: JsonPropertyName("token")] string Token);

internal record RegisterOrderRequest(
    [property: JsonPropertyName("auth_token")] string AuthToken,
    [property: JsonPropertyName("delivery_needed")] bool DeliveryNeeded,
    [property: JsonPropertyName("amount_cents")] long AmountCents,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("merchant_order_id")] string MerchantOrderId,
    [property: JsonPropertyName("items")] object[] Items);

internal record RegisterOrderResponse([property: JsonPropertyName("id")] long Id);

internal record BillingData(
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("last_name")] string LastName,
    [property: JsonPropertyName("phone_number")] string PhoneNumber,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("apartment")] string Apartment,
    [property: JsonPropertyName("floor")] string Floor,
    [property: JsonPropertyName("street")] string Street,
    [property: JsonPropertyName("building")] string Building,
    [property: JsonPropertyName("city")] string City,
    [property: JsonPropertyName("country")] string Country,
    [property: JsonPropertyName("state")] string State);

internal record PaymentKeyRequest(
    [property: JsonPropertyName("auth_token")] string AuthToken,
    [property: JsonPropertyName("amount_cents")] long AmountCents,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("order_id")] long OrderId,
    [property: JsonPropertyName("billing_data")] BillingData BillingData,
    [property: JsonPropertyName("integration_id")] int IntegrationId);

internal record PaymentKeyResponse([property: JsonPropertyName("token")] string Token);

/// The transaction-processed callback body: `{ type, obj: { ... } }`. HMAC verification
/// concatenates specific fields from `obj` (see PaymobHmac) — this record only needs enough of
/// `obj` to build our own VerifiedPaymentWebhook afterward.
internal record WebhookEnvelope([property: JsonPropertyName("obj")] WebhookTransactionObj? Obj);

internal record WebhookTransactionObj(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("amount_cents")] long AmountCents,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("order")] WebhookOrderObj? Order);

internal record WebhookOrderObj([property: JsonPropertyName("merchant_order_id")] string? MerchantOrderId);
