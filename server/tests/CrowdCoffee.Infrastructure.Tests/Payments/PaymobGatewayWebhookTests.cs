using System.Security.Cryptography;
using System.Text;
using CrowdCoffee.Infrastructure.Configuration;
using CrowdCoffee.Infrastructure.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CrowdCoffee.Infrastructure.Tests.Payments;

/// <summary>
/// Pure unit tests (no DB, no network) for PaymobGateway.VerifyAndParseWebhook — proves the HMAC
/// verification actually rejects a tampered/missing signature and accepts a correctly-signed one,
/// independent of PaymobHmac's field order matching Paymob's real docs exactly (that part is
/// flagged as unverified against a live sandbox in PaymobHmac's own doc comment).
/// </summary>
public class PaymobGatewayWebhookTests
{
    private const string HmacSecret = "test-hmac-secret";
    private static readonly Guid OrderId = Guid.NewGuid();

    private static PaymobGateway CreateGateway() =>
        new(new HttpClient(), Options.Create(new PaymobOptions { HmacSecret = HmacSecret }), NullLogger<PaymobGateway>.Instance);

    private static string BuildBody(long transactionId, long amountCents, bool success, string merchantOrderId) =>
        "{\"type\":\"TRANSACTION\",\"obj\":{" +
        $"\"amount_cents\":{amountCents}," +
        "\"created_at\":\"2026-01-01T00:00:00Z\",\"currency\":\"EGP\",\"error_occured\":false,\"has_parent_transaction\":false," +
        $"\"id\":{transactionId}," +
        "\"integration_id\":1,\"is_3d_secure\":true,\"is_auth\":false,\"is_capture\":false,\"is_refunded\":false,\"is_standalone_payment\":true,\"is_voided\":false," +
        $"\"order\":{{\"id\":9999,\"merchant_order_id\":\"{merchantOrderId}\"}}," +
        "\"owner\":123,\"pending\":false,\"source_data\":{\"pan\":\"1234\",\"sub_type\":\"Visa\",\"type\":\"card\"}," +
        $"\"success\":{(success ? "true" : "false")}" +
        "}}";

    // Mirrors PaymobHmac's exact field order/formatting, spelled out value-by-value against the
    // fixture in BuildBody — an independent computation the production code is checked against,
    // not a call into the internal helper itself.
    private static string ComputeHmac(long transactionId, long amountCents, bool success)
    {
        var concatenated =
            $"{amountCents}" + "2026-01-01T00:00:00Z" + "EGP" + "false" + "false" +
            $"{transactionId}" + "1" + "true" + "false" + "false" + "false" + "true" + "false" +
            "9999" + "123" + "false" + "1234" + "Visa" + "card" + (success ? "true" : "false");

        var hash = HMACSHA512.HashData(Encoding.UTF8.GetBytes(HmacSecret), Encoding.UTF8.GetBytes(concatenated));
        return Convert.ToHexString(hash);
    }

    [Fact]
    public void ValidSignature_ParsesTransactionCorrectly()
    {
        var gateway = CreateGateway();
        var body = BuildBody(555, 19000, success: true, OrderId.ToString());
        var hmac = ComputeHmac(555, 19000, success: true);

        var result = gateway.VerifyAndParseWebhook(body, hmac);

        Assert.True(result.IsSuccess);
        Assert.Equal("555", result.Value.GatewayTransactionId);
        Assert.Equal(OrderId, result.Value.OrderId);
        Assert.True(result.Value.IsSuccessful);
        Assert.Equal(190m, result.Value.AmountEgp);
    }

    [Fact]
    public void MissingSignature_IsRejected()
    {
        var gateway = CreateGateway();
        var body = BuildBody(555, 19000, success: true, OrderId.ToString());

        var result = gateway.VerifyAndParseWebhook(body, signature: null);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.InvalidSignature", result.Error!.Code);
    }

    [Fact]
    public void TamperedSignature_IsRejected()
    {
        var gateway = CreateGateway();
        var body = BuildBody(555, 19000, success: true, OrderId.ToString());

        var result = gateway.VerifyAndParseWebhook(body, signature: "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.InvalidSignature", result.Error!.Code);
    }

    [Fact]
    public void TamperedAmount_AfterSigning_IsRejected()
    {
        var gateway = CreateGateway();
        // Signature computed over 19000 cents, but body claims 100 cents — a would-be attacker
        // trying to report a smaller/larger charge than what was actually signed.
        var hmacForOriginalAmount = ComputeHmac(555, 19000, success: true);
        var tamperedBody = BuildBody(555, 100, success: true, OrderId.ToString());

        var result = gateway.VerifyAndParseWebhook(tamperedBody, hmacForOriginalAmount);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.InvalidSignature", result.Error!.Code);
    }

    [Fact]
    public void UnrecognizableMerchantOrderId_IsRejected()
    {
        var gateway = CreateGateway();
        var body = BuildBody(555, 19000, success: true, "not-a-guid");
        var hmac = ComputeHmac(555, 19000, success: true);

        var result = gateway.VerifyAndParseWebhook(body, hmac);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.UnknownOrder", result.Error!.Code);
    }
}
