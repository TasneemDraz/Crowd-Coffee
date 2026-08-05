using CrowdCoffee.Domain.Catalog;

namespace CrowdCoffee.Application.Catalog;

public record CategoryDto(Guid Id, string Slug, string Label, string? Description, int DisplayOrder);

/// <summary>
/// Raw catalog-structure projection, before Epic 4's computed (recipe-vs-stock) availability is
/// folded in. <see cref="IsManuallyAvailable"/> mirrors <see cref="Product.IsAvailable"/> as
/// stored — GetProductsHandler/GetProductModifiersHandler AND this with the live computed
/// availability to build the API-facing <see cref="ProductDto"/>.
/// </summary>
public record ProductCatalogRow(
    Guid Id,
    string CategorySlug,
    string Slug,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    int DisplayOrder,
    bool IsManuallyAvailable);

public record ProductDto(
    Guid Id,
    string CategorySlug,
    string Slug,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    int DisplayOrder,
    bool IsAvailable);

/// <see cref="IsManuallyActive"/> mirrors <see cref="ModifierOption.IsActive"/> — see
/// <see cref="ProductCatalogRow"/> for why this stays a separate pre-merge shape.
public record ModifierOptionCatalogRow(Guid Id, string Slug, string Name, decimal PriceDelta, int DisplayOrder, bool IsManuallyActive);

public record ModifierGroupCatalogRow(
    Guid Id,
    string Slug,
    string Name,
    ModifierSelectionType SelectionType,
    int MaxSelections,
    bool IsRequired,
    int DisplayOrder,
    IReadOnlyList<ModifierOptionCatalogRow> Options);

public record ModifierOptionDto(Guid Id, string Slug, string Name, decimal PriceDelta, int DisplayOrder, bool IsAvailable);

public record ModifierGroupDto(
    Guid Id,
    string Slug,
    string Name,
    string SelectionType,
    int MaxSelections,
    bool IsRequired,
    int DisplayOrder,
    IReadOnlyList<ModifierOptionDto> Options);
