using CrowdCoffee.Application.Inventory;
using CrowdCoffee.Domain.Inventory;

namespace CrowdCoffee.Application.Tests.Inventory;

public class AvailabilityCalculatorTests
{
    private static StockItem CreateStockItem(decimal onHand, decimal reserved = 0)
    {
        var item = new StockItem(Guid.NewGuid(), "Beans", StockUnit.Gram, reorderPoint: 10, initialQuantityOnHand: onHand);
        if (reserved > 0)
        {
            item.Reserve(reserved);
        }

        return item;
    }

    [Fact]
    public void NoRecipes_IsVacuouslyAvailable()
    {
        var result = AvailabilityCalculator.IsAvailable([], new Dictionary<Guid, StockItem>());

        Assert.True(result);
    }

    [Fact]
    public void SingleRecipe_ExactlyEnoughStock_IsAvailable()
    {
        var stockItem = CreateStockItem(onHand: 18);
        var recipe = Recipe.ForProduct(Guid.NewGuid(), Guid.NewGuid(), stockItem.Id, quantityPerUnit: 18);

        var result = AvailabilityCalculator.IsAvailable([recipe], new Dictionary<Guid, StockItem> { [stockItem.Id] = stockItem });

        Assert.True(result);
    }

    [Fact]
    public void SingleRecipe_OneUnitShortOfNeeded_IsUnavailable()
    {
        var stockItem = CreateStockItem(onHand: 17);
        var recipe = Recipe.ForProduct(Guid.NewGuid(), Guid.NewGuid(), stockItem.Id, quantityPerUnit: 18);

        var result = AvailabilityCalculator.IsAvailable([recipe], new Dictionary<Guid, StockItem> { [stockItem.Id] = stockItem });

        Assert.False(result);
    }

    [Fact]
    public void SingleRecipe_ZeroStock_IsUnavailable()
    {
        var stockItem = CreateStockItem(onHand: 0);
        var recipe = Recipe.ForProduct(Guid.NewGuid(), Guid.NewGuid(), stockItem.Id, quantityPerUnit: 1);

        var result = AvailabilityCalculator.IsAvailable([recipe], new Dictionary<Guid, StockItem> { [stockItem.Id] = stockItem });

        Assert.False(result);
    }

    [Fact]
    public void MultipleRecipes_AllSufficient_IsAvailable()
    {
        var beans = CreateStockItem(onHand: 100);
        var milk = CreateStockItem(onHand: 500);
        var productId = Guid.NewGuid();
        var recipes = new[]
        {
            Recipe.ForProduct(Guid.NewGuid(), productId, beans.Id, quantityPerUnit: 18),
            Recipe.ForProduct(Guid.NewGuid(), productId, milk.Id, quantityPerUnit: 200)
        };

        var result = AvailabilityCalculator.IsAvailable(recipes, new Dictionary<Guid, StockItem> { [beans.Id] = beans, [milk.Id] = milk });

        Assert.True(result);
    }

    [Fact]
    public void MultipleRecipes_OneInsufficient_IsUnavailable()
    {
        var beans = CreateStockItem(onHand: 100);
        var milk = CreateStockItem(onHand: 50); // needs 200, only has 50
        var productId = Guid.NewGuid();
        var recipes = new[]
        {
            Recipe.ForProduct(Guid.NewGuid(), productId, beans.Id, quantityPerUnit: 18),
            Recipe.ForProduct(Guid.NewGuid(), productId, milk.Id, quantityPerUnit: 200)
        };

        var result = AvailabilityCalculator.IsAvailable(recipes, new Dictionary<Guid, StockItem> { [beans.Id] = beans, [milk.Id] = milk });

        Assert.False(result);
    }

    [Fact]
    public void ReservedStock_ReducesAvailable_CanTipIntoUnavailable()
    {
        // 20 on hand, 15 already reserved by other orders -> only 5 truly Available.
        var stockItem = CreateStockItem(onHand: 20, reserved: 15);
        var recipe = Recipe.ForProduct(Guid.NewGuid(), Guid.NewGuid(), stockItem.Id, quantityPerUnit: 18);

        var result = AvailabilityCalculator.IsAvailable([recipe], new Dictionary<Guid, StockItem> { [stockItem.Id] = stockItem });

        Assert.False(result);
    }

    [Fact]
    public void RecipeReferencesMissingStockItem_IsUnavailable()
    {
        var recipe = Recipe.ForProduct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), quantityPerUnit: 1);

        var result = AvailabilityCalculator.IsAvailable([recipe], new Dictionary<Guid, StockItem>());

        Assert.False(result);
    }
}
