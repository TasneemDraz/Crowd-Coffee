namespace CrowdCoffee.Application.Catalog;

// Admin-facing shapes: unlike CatalogDtos' customer-facing rows (ProductCatalogRow etc., always
// active-only), these include IsActive so admin listing/editing can see and toggle deactivated
// rows, not just what a customer would see on the menu.

public record AdminCategoryDto(Guid Id, string Slug, string Label, string? Description, int DisplayOrder, bool IsActive);

public record CreateCategoryRequest(string Slug, string Label, string? Description, int DisplayOrder);

public record UpdateCategoryRequest(string Label, string? Description, int DisplayOrder, bool IsActive);

public record AdminProductDto(
    Guid Id,
    string CategorySlug,
    string Slug,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    int DisplayOrder,
    bool IsAvailable,
    bool IsActive);

public record CreateProductRequest(string CategorySlug, string Slug, string Name, string? Description, decimal Price, int DisplayOrder, string? ImageUrl);

public record UpdateProductRequest(string Name, string? Description, decimal Price, string? ImageUrl, int DisplayOrder, bool IsAvailable, bool IsActive);

public record AdminModifierOptionDto(Guid Id, string Slug, string Name, decimal PriceDelta, int DisplayOrder, bool IsActive);

public record AdminModifierGroupDto(
    Guid Id,
    string Slug,
    string Name,
    string SelectionType,
    int MaxSelections,
    bool IsRequired,
    int DisplayOrder,
    IReadOnlyList<AdminModifierOptionDto> Options);

/// Story 13.2's AC is scoped to "price deltas/active state" only — not renaming an option or
/// moving it between groups, so the request stays this narrow rather than a full-entity PUT.
public record UpdateModifierOptionRequest(decimal PriceDelta, bool IsActive);
