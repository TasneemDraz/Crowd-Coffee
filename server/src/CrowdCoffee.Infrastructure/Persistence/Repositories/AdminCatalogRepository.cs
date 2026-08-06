using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace CrowdCoffee.Infrastructure.Persistence.Repositories;

public class AdminCatalogRepository(CrowdCoffeeDbContext dbContext) : IAdminCatalogRepository
{
    public async Task<IReadOnlyList<AdminCategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken) =>
        await dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new AdminCategoryDto(c.Id, c.Slug, c.Label, c.Description, c.DisplayOrder, c.IsActive))
            .ToListAsync(cancellationToken);

    public Task<Category?> GetCategoryAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> CategorySlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        dbContext.Categories.AnyAsync(c => c.Slug == slug, cancellationToken);

    public void AddCategory(Category category) => dbContext.Categories.Add(category);

    public async Task<IReadOnlyList<AdminProductDto>> GetAllProductsAsync(CancellationToken cancellationToken) =>
        await dbContext.Products
            .AsNoTracking()
            .OrderBy(p => p.CategorySlug).ThenBy(p => p.DisplayOrder)
            .Select(p => new AdminProductDto(
                p.Id, p.CategorySlug, p.Slug, p.Name, p.Description, p.Price, p.ImageUrl,
                p.DisplayOrder, p.IsAvailable, p.IsActive))
            .ToListAsync(cancellationToken);

    public Task<Product?> GetProductAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ProductSlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        dbContext.Products.AnyAsync(p => p.Slug == slug, cancellationToken);

    public void AddProduct(Product product) => dbContext.Products.Add(product);

    public async Task<IReadOnlyList<ModifierGroup>> GetAllModifierGroupsAsync(CancellationToken cancellationToken) =>
        await dbContext.ModifierGroups
            .Include(g => g.Options)
            .OrderBy(g => g.DisplayOrder)
            .ToListAsync(cancellationToken);

    public Task<ModifierOption?> GetModifierOptionAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.ModifierOptions.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
