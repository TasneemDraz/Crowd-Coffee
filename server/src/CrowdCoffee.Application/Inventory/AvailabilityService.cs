using CrowdCoffee.Domain.Catalog;

namespace CrowdCoffee.Application.Inventory;

public class AvailabilityService(IAvailabilityRepository repository) : IAvailabilityService
{
    public async Task<bool> IsProductAvailableAsync(Product product, CancellationToken cancellationToken)
    {
        if (!product.IsAvailable)
        {
            return false;
        }

        var recipes = await repository.GetRecipesForProductAsync(product.Id, cancellationToken);
        if (recipes.Count == 0)
        {
            return true;
        }

        var stockItemIds = recipes.Select(r => r.StockItemId).Distinct().ToList();
        var stockItemsById = await repository.GetStockItemsByIdAsync(stockItemIds, cancellationToken);

        return AvailabilityCalculator.IsAvailable(recipes, stockItemsById);
    }

    public async Task<bool> IsModifierOptionAvailableAsync(ModifierOption modifierOption, CancellationToken cancellationToken)
    {
        if (!modifierOption.IsActive)
        {
            return false;
        }

        var recipes = await repository.GetRecipesForModifierOptionAsync(modifierOption.Id, cancellationToken);
        if (recipes.Count == 0)
        {
            return true;
        }

        var stockItemIds = recipes.Select(r => r.StockItemId).Distinct().ToList();
        var stockItemsById = await repository.GetStockItemsByIdAsync(stockItemIds, cancellationToken);

        return AvailabilityCalculator.IsAvailable(recipes, stockItemsById);
    }

    public async Task<IReadOnlyDictionary<Guid, bool>> GetComputedAvailabilityForProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, bool>();
        }

        var recipes = await repository.GetRecipesForProductsAsync(productIds, cancellationToken);
        var stockItemsById = await repository.GetStockItemsByIdAsync(recipes.Select(r => r.StockItemId).Distinct().ToList(), cancellationToken);
        var recipesByOwner = recipes.ToLookup(r => r.ProductId!.Value);

        return productIds.ToDictionary(id => id, id => AvailabilityCalculator.IsAvailable(recipesByOwner[id].ToList(), stockItemsById));
    }

    public async Task<IReadOnlyDictionary<Guid, bool>> GetComputedAvailabilityForModifierOptionsAsync(IReadOnlyCollection<Guid> modifierOptionIds, CancellationToken cancellationToken)
    {
        if (modifierOptionIds.Count == 0)
        {
            return new Dictionary<Guid, bool>();
        }

        var recipes = await repository.GetRecipesForModifierOptionsAsync(modifierOptionIds, cancellationToken);
        var stockItemsById = await repository.GetStockItemsByIdAsync(recipes.Select(r => r.StockItemId).Distinct().ToList(), cancellationToken);
        var recipesByOwner = recipes.ToLookup(r => r.ModifierOptionId!.Value);

        return modifierOptionIds.ToDictionary(id => id, id => AvailabilityCalculator.IsAvailable(recipesByOwner[id].ToList(), stockItemsById));
    }
}
