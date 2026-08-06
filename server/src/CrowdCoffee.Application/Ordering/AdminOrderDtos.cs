using CrowdCoffee.Domain.Ordering;

namespace CrowdCoffee.Application.Ordering;

/// DateFrom/DateTo are compared against Order.CreatedAt (UTC) directly, not shop-local (Cairo)
/// day boundaries — unlike OrderNumber's daily sequence, which genuinely needs exact shop-day
/// semantics, an admin filter is off by at most a couple of hours at the boundary, which is an
/// acceptable simplification here.
public record OrderSearchFilters(DateOnly? DateFrom, DateOnly? DateTo, OrderStatus? Status, OrderMode? Mode, PaymentMethod? PaymentMethod);

public record AdminOrderSummaryDto(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string Mode,
    string PaymentMethod,
    string Status,
    decimal Total,
    DateTime CreatedAt);

public record OrderStatusHistoryDto(string FromStatus, string ToStatus, string ChangedBy, DateTime ChangedAt, string? Reason);

public record AdminOrderLineDto(string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal, IReadOnlyList<string> Modifiers);

public record AdminOrderDetailDto(
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
    DateTime? ConfirmedAt,
    DateTime? CompletedAt,
    string? Notes,
    IReadOnlyList<AdminOrderLineDto> Lines,
    IReadOnlyList<OrderStatusHistoryDto> StatusHistory);
