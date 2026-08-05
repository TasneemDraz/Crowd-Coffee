using CrowdCoffee.Application.Inventory;
using CrowdCoffee.Domain.Inventory;

namespace CrowdCoffee.Application.Tests.Inventory;

/// <summary>
/// Covers the bulk (per-id-dictionary) overloads added for Epic 5's catalog list — the
/// single-item overloads already have call-site coverage via AvailabilityCalculatorTests plus
/// the Epic 4 integration tests. FakeAvailabilityRepository stands in for EF here since nothing
/// in this project references a database (matches this test project's existing no-mocking-
/// library convention).
/// </summary>
public class AvailabilityServiceBulkTests
{
    private static StockItem CreateStockItem(decimal onHand)
        => new(Guid.NewGuid(), "Beans", StockUnit.Gram, reorderPoint: 10, initialQuantityOnHand: onHand);

    [Fact]
    public async Task GetComputedAvailabilityForProducts_ReturnsPerProductResult_InTwoQueriesWorthOfData()
    {
        var plentifulBeans = CreateStockItem(onHand: 1000);
        var scarceMilk = CreateStockItem(onHand: 10);

        var wellStockedProduct = Guid.NewGuid();
        var shortStockedProduct = Guid.NewGuid();
        var noRecipeProduct = Guid.NewGuid();

        var repository = new FakeAvailabilityRepository(
            recipes:
            [
                Recipe.ForProduct(Guid.NewGuid(), wellStockedProduct, plentifulBeans.Id, quantityPerUnit: 18),
                Recipe.ForProduct(Guid.NewGuid(), shortStockedProduct, scarceMilk.Id, quantityPerUnit: 200)
            ],
            stockItems: [plentifulBeans, scarceMilk]);

        var service = new AvailabilityService(repository);

        var result = await service.GetComputedAvailabilityForProductsAsync(
            [wellStockedProduct, shortStockedProduct, noRecipeProduct], CancellationToken.None);

        Assert.True(result[wellStockedProduct]);
        Assert.False(result[shortStockedProduct]);
        Assert.True(result[noRecipeProduct]); // zero recipe rows -> vacuously available
    }

    [Fact]
    public async Task GetComputedAvailabilityForModifierOptions_ReturnsPerOptionResult()
    {
        var oatMilk = CreateStockItem(onHand: 0);
        var wellStockedOption = Guid.NewGuid();
        var outOfStockOption = Guid.NewGuid();

        var repository = new FakeAvailabilityRepository(
            recipes: [Recipe.ForModifierOption(Guid.NewGuid(), outOfStockOption, oatMilk.Id, quantityPerUnit: 1)],
            stockItems: [oatMilk]);

        var service = new AvailabilityService(repository);

        var result = await service.GetComputedAvailabilityForModifierOptionsAsync(
            [wellStockedOption, outOfStockOption], CancellationToken.None);

        Assert.True(result[wellStockedOption]);
        Assert.False(result[outOfStockOption]);
    }

    [Fact]
    public async Task GetComputedAvailabilityForProducts_EmptyInput_ReturnsEmptyDictionary()
    {
        var repository = new FakeAvailabilityRepository(recipes: [], stockItems: []);
        var service = new AvailabilityService(repository);

        var result = await service.GetComputedAvailabilityForProductsAsync([], CancellationToken.None);

        Assert.Empty(result);
    }

    private class FakeAvailabilityRepository(IReadOnlyList<Recipe> recipes, IReadOnlyList<StockItem> stockItems) : IAvailabilityRepository
    {
        public Task<IReadOnlyList<Recipe>> GetRecipesForProductAsync(Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Recipe>>(recipes.Where(r => r.ProductId == productId).ToList());

        public Task<IReadOnlyList<Recipe>> GetRecipesForModifierOptionAsync(Guid modifierOptionId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Recipe>>(recipes.Where(r => r.ModifierOptionId == modifierOptionId).ToList());

        public Task<IReadOnlyList<Recipe>> GetRecipesForProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Recipe>>(recipes.Where(r => r.ProductId is not null && productIds.Contains(r.ProductId.Value)).ToList());

        public Task<IReadOnlyList<Recipe>> GetRecipesForModifierOptionsAsync(IReadOnlyCollection<Guid> modifierOptionIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Recipe>>(recipes.Where(r => r.ModifierOptionId is not null && modifierOptionIds.Contains(r.ModifierOptionId.Value)).ToList());

        public Task<IReadOnlyDictionary<Guid, StockItem>> GetStockItemsByIdAsync(IReadOnlyCollection<Guid> stockItemIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, StockItem>>(stockItems.Where(s => stockItemIds.Contains(s.Id)).ToDictionary(s => s.Id));
    }
}
