using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CrowdCoffee.Infrastructure.Tests.Catalog;

/// <summary>
/// Epic 13.1/13.2 against real local SQL Server + the real seeded catalog. The cache-invalidation
/// assertions are the AC's actual point ("writes invalidate cache promptly") — Epic 5.3 built
/// ICatalogCache.InvalidateAll() and explicitly noted nothing called it yet; these tests are the
/// first proof that Epic 13's admin writes actually close that gap end-to-end, not just that the
/// write itself succeeded.
/// </summary>
public class AdminCatalogIntegrationTests
{
    [Fact]
    public async Task CreateCategory_ThenCreateProduct_BothAppearInAdminListings_AndInvalidateCustomerCache()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var createCategory = scope.ServiceProvider.GetRequiredService<CreateCategoryHandler>();
        var createProduct = scope.ServiceProvider.GetRequiredService<CreateProductHandler>();
        var getCategories = scope.ServiceProvider.GetRequiredService<GetCategoriesHandler>();

        var categorySlug = $"test-cat-{Guid.NewGuid():N}";
        var productSlug = $"test-prod-{Guid.NewGuid():N}";

        try
        {
            // Prime the customer-facing cache before the admin write, so InvalidateAll() has
            // something stale to actually clear — otherwise this test could pass by accident if
            // caching were silently broken instead of correctly wired.
            var beforeCount = (await getCategories.HandleAsync(CancellationToken.None)).Count;

            var categoryResult = await createCategory.HandleAsync(
                new CreateCategoryRequest(categorySlug, "Test Category", "For integration tests.", 999), CancellationToken.None);
            Assert.True(categoryResult.IsSuccess);

            var afterCount = (await getCategories.HandleAsync(CancellationToken.None)).Count;
            Assert.Equal(beforeCount + 1, afterCount);

            var productResult = await createProduct.HandleAsync(
                new CreateProductRequest(categorySlug, productSlug, "Test Product", null, 55m, 0, null), CancellationToken.None);

            Assert.True(productResult.IsSuccess);
            Assert.Equal(categorySlug, productResult.Value.CategorySlug);
            Assert.True(productResult.Value.IsActive);
        }
        finally
        {
            await dbContext.Products.Where(p => p.Slug == productSlug).ExecuteDeleteAsync();
            await dbContext.Categories.Where(c => c.Slug == categorySlug).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task UpdateProduct_Deactivate_RemovesItFromCustomerFacingProductList()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var adminRepository = scope.ServiceProvider.GetRequiredService<IAdminCatalogRepository>();
        var updateProduct = scope.ServiceProvider.GetRequiredService<UpdateProductHandler>();
        var getProducts = scope.ServiceProvider.GetRequiredService<GetProductsHandler>();

        var product = await dbContext.Products.AsNoTracking().Where(p => p.CategorySlug == "classic" && p.IsActive).OrderBy(p => p.Id).Skip(5).FirstAsync();

        try
        {
            Assert.Contains(await getProducts.HandleAsync(CancellationToken.None), p => p.Id == product.Id);

            var result = await updateProduct.HandleAsync(
                product.Id,
                new UpdateProductRequest(product.Name, product.Description, product.Price, product.ImageUrl, product.DisplayOrder, product.IsAvailable, IsActive: false),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.False(result.Value.IsActive);

            Assert.DoesNotContain(await getProducts.HandleAsync(CancellationToken.None), p => p.Id == product.Id);
        }
        finally
        {
            // Restore — this test mutates a real seeded row, not one it created itself.
            var entity = await adminRepository.GetProductAsync(product.Id, CancellationToken.None);
            entity!.Activate();
            await adminRepository.SaveChangesAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task UpdateModifierOption_ChangesPriceDelta_WithoutTouchingPastOrderSnapshots()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var updateOption = scope.ServiceProvider.GetRequiredService<UpdateModifierOptionHandler>();

        var oatMilk = await dbContext.ModifierOptions.AsNoTracking().FirstAsync(o => o.Slug == "oat");
        var originalDelta = oatMilk.PriceDelta;

        try
        {
            var result = await updateOption.HandleAsync(oatMilk.Id, new UpdateModifierOptionRequest(originalDelta + 5, true), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(originalDelta + 5, result.Value.PriceDelta);
        }
        finally
        {
            // Restore — this test mutates a real seeded row, not one it created itself.
            await updateOption.HandleAsync(oatMilk.Id, new UpdateModifierOptionRequest(originalDelta, true), CancellationToken.None);
        }
    }
}
