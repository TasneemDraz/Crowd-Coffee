using CrowdCoffee.Domain.Inventory;

namespace CrowdCoffee.Application.Inventory;

/// <summary>
/// Pure recipe-vs-stock decision (§8): unavailable the moment any one ingredient can't cover one
/// unit. A product/option with zero recipe rows is treated as available — the real per-drink
/// recipe quantities aren't in the design data yet (flagged TBD in Epic 2, Story 2.5), and it
/// would be wrong for the entire menu to go unavailable just because that data hasn't landed.
/// </summary>
public static class AvailabilityCalculator
{
    public static bool IsAvailable(IReadOnlyCollection<Recipe> recipes, IReadOnlyDictionary<Guid, StockItem> stockItemsById)
    {
        if (recipes.Count == 0)
        {
            return true;
        }

        return recipes.All(recipe =>
            stockItemsById.TryGetValue(recipe.StockItemId, out var stockItem) &&
            stockItem.Available >= recipe.QuantityPerUnit);
    }
}
