using CrowdCoffee.Application.Catalog;
using Microsoft.EntityFrameworkCore;

namespace CrowdCoffee.Infrastructure.Persistence.Repositories;

/// <summary>
/// Read-only projections straight into DTOs (AsNoTracking, no navigation loads beyond the one
/// collection Story 5.2 actually needs) — Epic 5's <200ms budget doesn't afford materializing
/// full Product/ModifierGroup aggregates just to read a handful of columns.
/// </summary>
public class CatalogRepository(CrowdCoffeeDbContext dbContext) : ICatalogRepository
{
    public async Task<IReadOnlyList<CategoryDto>> GetActiveCategoriesAsync(CancellationToken cancellationToken) =>
        await dbContext.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto(c.Id, c.Slug, c.Label, c.Description, c.DisplayOrder))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductCatalogRow>> GetActiveProductsAsync(CancellationToken cancellationToken) =>
        await dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new ProductCatalogRow(p.Id, p.CategorySlug, p.Slug, p.Name, p.Description, p.Price, p.ImageUrl, p.DisplayOrder, p.IsAvailable))
            .ToListAsync(cancellationToken);

    public async Task<ProductCatalogRow?> GetActiveProductBySlugAsync(string slug, CancellationToken cancellationToken) =>
        await dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Slug == slug)
            .Select(p => new ProductCatalogRow(p.Id, p.CategorySlug, p.Slug, p.Name, p.Description, p.Price, p.ImageUrl, p.DisplayOrder, p.IsAvailable))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ModifierGroupCatalogRow>> GetModifierGroupsForCategoryAsync(string categorySlug, CancellationToken cancellationToken) =>
        await dbContext.ModifierGroups
            .AsNoTracking()
            .Where(g => dbContext.ProductModifierGroups.Any(pmg => pmg.CategorySlug == categorySlug && pmg.ModifierGroupId == g.Id))
            .OrderBy(g => g.DisplayOrder)
            .Select(g => new ModifierGroupCatalogRow(
                g.Id,
                g.Slug,
                g.Name,
                g.SelectionType,
                g.MaxSelections,
                g.IsRequired,
                g.DisplayOrder,
                g.Options
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => new ModifierOptionCatalogRow(o.Id, o.Slug, o.Name, o.PriceDelta, o.DisplayOrder, o.IsActive))
                    .ToList()))
            .ToListAsync(cancellationToken);
}
