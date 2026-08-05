namespace CrowdCoffee.Application.Catalog;

public interface ICatalogRepository
{
    Task<IReadOnlyList<CategoryDto>> GetActiveCategoriesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductCatalogRow>> GetActiveProductsAsync(CancellationToken cancellationToken);

    Task<ProductCatalogRow?> GetActiveProductBySlugAsync(string slug, CancellationToken cancellationToken);

    /// Epic 8's cart re-pricing endpoint identifies products by id (the client already has it
    /// from the products list), not slug.
    Task<ProductCatalogRow?> GetActiveProductByIdAsync(Guid id, CancellationToken cancellationToken);

    /// Modifier groups are category-wide, not per-product (§4a DRINK_CATS) — see
    /// ProductModifierGroup for that decision.
    Task<IReadOnlyList<ModifierGroupCatalogRow>> GetModifierGroupsForCategoryAsync(string categorySlug, CancellationToken cancellationToken);
}
