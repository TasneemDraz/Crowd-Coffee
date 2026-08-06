using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Domain.Catalog;

namespace CrowdCoffee.Application.Tests.Catalog;

public class CreateCategoryHandlerTests
{
    [Fact]
    public async Task NewSlug_CreatesCategory_AndInvalidatesCache()
    {
        var repository = new FakeAdminCatalogRepository();
        var cache = new FakeCatalogCache();
        var handler = new CreateCategoryHandler(repository, cache);

        var result = await handler.HandleAsync(
            new CreateCategoryRequest("pastries", "Pastries", "Fresh baked.", 5), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("pastries", result.Value.Slug);
        Assert.True(result.Value.IsActive);
        Assert.Equal(1, cache.InvalidateCallCount);

        var all = await repository.GetAllCategoriesAsync(CancellationToken.None);
        Assert.Single(all);
    }

    [Fact]
    public async Task DuplicateSlug_IsRejected_AndDoesNotInvalidateCache()
    {
        var repository = new FakeAdminCatalogRepository(categories: [new Category(Guid.NewGuid(), "pastries", "Pastries", null, 0)]);
        var cache = new FakeCatalogCache();
        var handler = new CreateCategoryHandler(repository, cache);

        var result = await handler.HandleAsync(
            new CreateCategoryRequest("pastries", "Pastries Again", null, 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Category.SlugExists", result.Error!.Code);
        Assert.Equal(0, cache.InvalidateCallCount);
    }
}
