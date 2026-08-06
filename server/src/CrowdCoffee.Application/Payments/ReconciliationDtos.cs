namespace CrowdCoffee.Application.Payments;

/// Story 10.6: gateway-confirmed payments vs. orders, in one row per payment event. OrderNumber/
/// OrderTotal/OrderStatus are null only for the (should-be-rare) case of a webhook that arrived
/// for an order id this DB never had — surfacing that as a visible gap is the point, not
/// something to hide behind a join that silently drops the row.
public record ReconciliationRowDto(
    Guid OrderId,
    string? OrderNumber,
    decimal? OrderTotal,
    string? OrderStatus,
    string GatewayTransactionId,
    decimal GatewayAmountEgp,
    bool GatewaySuccessful,
    DateTime ReceivedAtUtc);
