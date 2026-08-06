using CrowdCoffee.Domain.Ordering;

namespace CrowdCoffee.Application.Ordering;

/// Shares CartLineRepricer's validation/pricing exactly — this is the same "what did the
/// customer actually select" shape as CartLineRepriceRequest (Application/Cart), just under the
/// Ordering module's own name since a cart line and an order line are different concepts even
/// though the repricing logic is identical.
public record CreateOrderLineRequest(Guid ProductId, Guid? MilkOptionId, Guid? ShotOptionId, List<Guid> SyrupOptionIds, int Quantity);

public record CreateOrderRequest(
    string CustomerName,
    string Phone,
    OrderMode Mode,
    string? Address,
    double? Latitude,
    double? Longitude,
    PaymentMethod PaymentMethod,
    string IdempotencyKey,
    List<CreateOrderLineRequest> Lines,
    string? Notes);

public record OrderLineDto(Guid ProductId, string Name, decimal UnitPrice, int Quantity, decimal LineTotal);

public record OrderDto(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string Phone,
    string Mode,
    string? Address,
    double? Latitude,
    double? Longitude,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal Total,
    string PaymentMethod,
    string PaymentStatus,
    string Status,
    DateTime CreatedAt,
    IReadOnlyList<OrderLineDto> Lines);
