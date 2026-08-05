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
}
