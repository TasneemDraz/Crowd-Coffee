using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Domain.Catalog;

namespace CrowdCoffee.Application.Tests.Catalog;

public class UpdateCategoryHandlerTests
{
    [Fact]
    public async Task ExistingCategory_UpdatesLabelDescriptionDisplayOrderAndActiveState()
    {
        var category = new Category(Guid.NewGuid(), "pastries", "Pastries", "Old description.", 5);
        var repository = new FakeAdminCatalogRepository(categories: [category]);
        var cache = new FakeCatalogCache();
        var handler = new UpdateCategoryHandler(repository, cache);

        var result = await handler.HandleAsync(
            category.Id, new UpdateCategoryRequest("Bakery", "New description.", 2, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Bakery", result.Value.Label);
        Assert.Equal("New description.", result.Value.Description);
        Assert.Equal(2, result.Value.DisplayOrder);
        Assert.False(result.Value.IsActive);
        Assert.Equal(1, cache.InvalidateCallCount);
    }

    [Fact]
    public async Task UnknownCategory_ReturnsNotFound()
    {
        var repository = new FakeAdminCatalogRepository();
        var cache = new FakeCatalogCache();
        var handler = new UpdateCategoryHandler(repository, cache);

        var result = await handler.HandleAsync(Guid.NewGuid(), new UpdateCategoryRequest("X", null, 0, true), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Category.NotFound", result.Error!.Code);
        Assert.Equal(0, cache.InvalidateCallCount);
    }
}
