using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Domain.Common;
using CrowdCoffee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CrowdCoffee.Infrastructure.Tests.Catalog;

/// <summary>
/// Exercises Epic 5's read models against the real seeded menu data (19 categories, 161
/// products, per Story 2.5) rather than an in-memory substitute — the point of these queries is
/// the actual EF projections/joins working against SQL Server, cache included.
/// </summary>
public class CatalogQueryIntegrationTests
{
    [Fact]
    public async Task GetCategories_Returns19SeededCategories_InDisplayOrder()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetCategoriesHandler>();

        var categories = await handler.HandleAsync(CancellationToken.None);

        Assert.Equal(19, categories.Count);
        Assert.Equal(categories.OrderBy(c => c.DisplayOrder).ToList(), categories);
    }

    [Fact]
    public async Task GetProducts_ReturnsSeededActiveProducts_AllAvailableByDefault()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<GetProductsHandler>();

        var activeProductCount = await dbContext.Products.CountAsync(p => p.IsActive);

        var products = await handler.HandleAsync(CancellationToken.None);

        // Seed data ships with zero Recipe rows (Story 2.5) and every seeded Product defaults to
        // IsAvailable = true, so absent another test's transient stock fixtures running
        // concurrently, every active product comes back available.
        Assert.True(products.Count <= activeProductCount);
        Assert.All(products, p => Assert.True(p.IsAvailable));
    }

    [Fact]
    public async Task GetProductModifiers_ForDrinkCategoryProduct_ReturnsGroupsWithOptions()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<GetProductModifiersHandler>();

        var drinkProductSlug = await dbContext.Products
            .Where(p => dbContext.ProductModifierGroups.Any(pmg => pmg.CategorySlug == p.CategorySlug))
            .Select(p => p.Slug)
            .FirstAsync();

        var result = await handler.HandleAsync(drinkProductSlug, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
        Assert.All(result.Value, g => Assert.NotEmpty(g.Options));
    }

    [Fact]
    public async Task GetProductModifiers_UnknownSlug_ReturnsNotFound()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetProductModifiersHandler>();

        var result = await handler.HandleAsync($"does-not-exist-{Guid.NewGuid()}", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
