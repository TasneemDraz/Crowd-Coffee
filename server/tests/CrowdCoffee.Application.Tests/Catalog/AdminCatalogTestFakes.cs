using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Application.Common;
using CrowdCoffee.Domain.Catalog;

namespace CrowdCoffee.Application.Tests.Catalog;

/// Shared by the admin catalog handler tests — matches this test project's no-mocking-library
/// convention (hand-rolled fakes standing in for EF-backed implementations).
internal class FakeAdminCatalogRepository : IAdminCatalogRepository
{
    private readonly List<Category> _categories = [];
    private readonly List<Product> _products = [];
    private readonly List<ModifierGroup> _modifierGroups = [];
    private readonly List<ModifierOption> _modifierOptions = [];

    public FakeAdminCatalogRepository(
        IEnumerable<Category>? categories = null,
        IEnumerable<Product>? products = null,
        IEnumerable<ModifierGroup>? modifierGroups = null,
        IEnumerable<ModifierOption>? modifierOptions = null)
    {
        if (categories is not null) _categories.AddRange(categories);
        if (products is not null) _products.AddRange(products);
        if (modifierGroups is not null) _modifierGroups.AddRange(modifierGroups);
        if (modifierOptions is not null) _modifierOptions.AddRange(modifierOptions);
    }

    public Task<IReadOnlyList<AdminCategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AdminCategoryDto>>(
            _categories.Select(c => new AdminCategoryDto(c.Id, c.Slug, c.Label, c.Description, c.DisplayOrder, c.IsActive)).ToList());

    public Task<Category?> GetCategoryAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_categories.FirstOrDefault(c => c.Id == id));

    public Task<bool> CategorySlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        Task.FromResult(_categories.Any(c => c.Slug == slug));

    public void AddCategory(Category category) => _categories.Add(category);

    public Task<IReadOnlyList<AdminProductDto>> GetAllProductsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AdminProductDto>>(
            _products.Select(p => new AdminProductDto(
                p.Id, p.CategorySlug, p.Slug, p.Name, p.Description, p.Price, p.ImageUrl, p.DisplayOrder, p.IsAvailable, p.IsActive)).ToList());

    public Task<Product?> GetProductAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_products.FirstOrDefault(p => p.Id == id));

    public Task<bool> ProductSlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        Task.FromResult(_products.Any(p => p.Slug == slug));

    public void AddProduct(Product product) => _products.Add(product);

    public Task<IReadOnlyList<ModifierGroup>> GetAllModifierGroupsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ModifierGroup>>(_modifierGroups);

    public Task<ModifierOption?> GetModifierOptionAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_modifierOptions.FirstOrDefault(o => o.Id == id));

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal class FakeCatalogCache : ICatalogCache
{
    public int InvalidateCallCount { get; private set; }

    public Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken) =>
        factory(cancellationToken);

    public void InvalidateAll() => InvalidateCallCount++;
}
