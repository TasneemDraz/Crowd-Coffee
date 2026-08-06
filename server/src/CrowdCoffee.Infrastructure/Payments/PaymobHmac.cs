using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CrowdCoffee.Infrastructure.Payments;

/// <summary>
/// Paymob's transaction-processed callback HMAC: sort is NOT alphabetical on the JSON — it's this
/// fixed field order (confirmed against Paymob's published HMAC documentation as of writing),
/// reading each value's raw string form out of the callback's `obj`, concatenated with no
/// separator, then HMAC-SHA512 with the integration's HMAC secret, hex-encoded. The `hmac` value
/// itself arrives as a query-string parameter on the callback URL, not a body field.
///
/// UNVERIFIED AGAINST A LIVE PAYMOB SANDBOX — this project has no merchant credentials (per user
/// decision, Epic 10 was built against the documented contract only). Before go-live, trigger a
/// real sandbox transaction and confirm a computed hash matches the `hmac` Paymob actually sends;
/// if it doesn't, the field order or value formatting below is the first thing to check.
/// </summary>
internal static class PaymobHmac
{
    private static readonly string[] FieldOrder =
    [
        "amount_cents", "created_at", "currency", "error_occured", "has_parent_transaction", "id",
        "integration_id", "is_3d_secure", "is_auth", "is_capture", "is_refunded", "is_standalone_payment",
        "is_voided", "order.id", "owner", "pending", "source_data.pan", "source_data.sub_type",
        "source_data.type", "success"
    ];

    public static bool Verify(JsonElement obj, string hmacSecret, string? providedHmac)
    {
        if (string.IsNullOrWhiteSpace(providedHmac))
        {
            return false;
        }

        var concatenated = new StringBuilder();
        foreach (var field in FieldOrder)
        {
            concatenated.Append(ReadRawValue(obj, field));
        }

        var keyBytes = Encoding.UTF8.GetBytes(hmacSecret);
        var messageBytes = Encoding.UTF8.GetBytes(concatenated.ToString());
        var computedHash = HMACSHA512.HashData(keyBytes, messageBytes);
        var computedHex = Convert.ToHexString(computedHash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHex),
            Encoding.UTF8.GetBytes(providedHmac.Trim().ToUpperInvariant()));
    }

    private static string ReadRawValue(JsonElement obj, string dottedPath)
    {
        var parts = dottedPath.Split('.');
        var current = obj;
        foreach (var part in parts)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out var next))
            {
                return string.Empty;
            }

            current = next;
        }

        return current.ValueKind switch
        {
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            JsonValueKind.String => current.GetString() ?? string.Empty,
            JsonValueKind.Number => current.GetRawText(),
            _ => string.Empty
        };
    }
}
