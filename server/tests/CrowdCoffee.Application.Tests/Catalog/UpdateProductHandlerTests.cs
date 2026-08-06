using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Domain.Catalog;

namespace CrowdCoffee.Application.Tests.Catalog;

public class UpdateProductHandlerTests
{
    [Fact]
    public async Task ExistingProduct_UpdatesFields_IncludingReactivation()
    {
        var product = new Product(Guid.NewGuid(), "classic", "c-latte", "Latte", "Old.", 100, 0);
        product.Deactivate();
        var repository = new FakeAdminCatalogRepository(products: [product]);
        var cache = new FakeCatalogCache();
        var handler = new UpdateProductHandler(repository, cache);

        var result = await handler.HandleAsync(
            product.Id,
            new UpdateProductRequest("Latte Supreme", "New.", 120, "/uploads/products/x.jpg", 3, false, true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Latte Supreme", result.Value.Name);
        Assert.Equal(120, result.Value.Price);
        Assert.Equal("/uploads/products/x.jpg", result.Value.ImageUrl);
        Assert.Equal(3, result.Value.DisplayOrder);
        Assert.False(result.Value.IsAvailable);
        Assert.True(result.Value.IsActive); // reactivated
        Assert.Equal(1, cache.InvalidateCallCount);
    }

    [Fact]
    public async Task UnknownProduct_ReturnsNotFound()
    {
        var repository = new FakeAdminCatalogRepository();
        var cache = new FakeCatalogCache();
        var handler = new UpdateProductHandler(repository, cache);

        var result = await handler.HandleAsync(
            Guid.NewGuid(), new UpdateProductRequest("X", null, 10, null, 0, true, true), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.NotFound", result.Error!.Code);
    }
}
