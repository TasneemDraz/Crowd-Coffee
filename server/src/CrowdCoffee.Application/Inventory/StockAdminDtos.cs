namespace CrowdCoffee.Application.Inventory;

public record StockItemSummaryDto(
    Guid Id,
    string Name,
    string Unit,
    decimal QuantityOnHand,
    decimal QuantityReserved,
    decimal Available,
    decimal ReorderPoint,
    bool IsActive,
    bool IsLowStock);

public record StockMovementDto(
    Guid Id,
    decimal Quantity,
    string MovementType,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Notes,
    string CreatedBy,
    DateTime CreatedAt);
