using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Domain.Catalog;

namespace CrowdCoffee.Application.Tests.Catalog;

public class CreateProductHandlerTests
{
    private static Category Classic => new(Guid.NewGuid(), "classic", "Classic", null, 0);

    [Fact]
    public async Task KnownCategory_NewSlug_CreatesProduct()
    {
        var category = Classic;
        var repository = new FakeAdminCatalogRepository(categories: [category]);
        var cache = new FakeCatalogCache();
        var handler = new CreateProductHandler(repository, cache);

        var result = await handler.HandleAsync(
            new CreateProductRequest(category.Slug, "c-latte", "Latte", "Espresso-based.", 100, 0, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("c-latte", result.Value.Slug);
        Assert.True(result.Value.IsActive);
        Assert.True(result.Value.IsAvailable);
        Assert.Equal(1, cache.InvalidateCallCount);
    }

    [Fact]
    public async Task UnknownCategory_IsRejected()
    {
        var repository = new FakeAdminCatalogRepository();
        var cache = new FakeCatalogCache();
        var handler = new CreateProductHandler(repository, cache);

        var result = await handler.HandleAsync(
            new CreateProductRequest("does-not-exist", "c-latte", "Latte", null, 100, 0, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.UnknownCategory", result.Error!.Code);
        Assert.Equal(0, cache.InvalidateCallCount);
    }

    [Fact]
    public async Task DuplicateProductSlug_IsRejected()
    {
        var category = Classic;
        var existing = new Product(Guid.NewGuid(), category.Slug, "c-latte", "Latte", null, 100, 0);
        var repository = new FakeAdminCatalogRepository(categories: [category], products: [existing]);
        var cache = new FakeCatalogCache();
        var handler = new CreateProductHandler(repository, cache);

        var result = await handler.HandleAsync(
            new CreateProductRequest(category.Slug, "c-latte", "Latte Again", null, 100, 0, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.SlugExists", result.Error!.Code);
    }
}
