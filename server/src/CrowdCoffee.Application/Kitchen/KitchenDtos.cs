namespace CrowdCoffee.Application.Kitchen;

/// AC (Story 12.2): "modifiers spelled out without extra tap" — Modifiers is a flat list of
/// display names (e.g. "Oat Milk", "Espresso Shot", "Vanilla Syrup"), not ids the board would
/// need a second lookup to render.
public record KitchenOrderLineDto(string ProductName, int Quantity, IReadOnlyList<string> Modifiers);

public record KitchenOrderDto(
    Guid Id,
    string OrderNumber,
    string Mode,
    string? Address,
    double? Latitude,
    double? Longitude,
    string CustomerName,
    string Phone,
    string Status,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    string? Notes,
    IReadOnlyList<KitchenOrderLineDto> Lines);
