using CrowdCoffee.Domain.Catalog;

namespace CrowdCoffee.Application.Catalog;

/// <summary>
/// Write-side counterpart to ICatalogRepository (Epic 5), which is deliberately a read-only,
/// AsNoTracking, active-only projection straight to DTOs — good for the <200ms customer menu
/// budget, wrong shape for admin writes that need to load a tracked entity, mutate it via its own
/// domain methods, and see inactive rows too. Kept as a separate interface rather than widening
/// ICatalogRepository so the customer-facing read path's contract doesn't grow admin-only concerns.
/// </summary>
public interface IAdminCatalogRepository
{
    Task<IReadOnlyList<AdminCategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken);

    /// Tracked — callers mutate the returned entity via its own methods and call SaveChangesAsync.
    Task<Category?> GetCategoryAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> CategorySlugExistsAsync(string slug, CancellationToken cancellationToken);

    void AddCategory(Category category);

    Task<IReadOnlyList<AdminProductDto>> GetAllProductsAsync(CancellationToken cancellationToken);

    Task<Product?> GetProductAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ProductSlugExistsAsync(string slug, CancellationToken cancellationToken);

    void AddProduct(Product product);

    /// Includes Options — admin's modifier screen edits options inline against their group, and
    /// the catalog is tiny (a handful of groups), so one query for the whole tree is fine.
    Task<IReadOnlyList<ModifierGroup>> GetAllModifierGroupsAsync(CancellationToken cancellationToken);

    Task<ModifierOption?> GetModifierOptionAsync(Guid id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
