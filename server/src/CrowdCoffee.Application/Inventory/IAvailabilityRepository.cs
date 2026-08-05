using CrowdCoffee.Domain.Inventory;

namespace CrowdCoffee.Application.Inventory;

public interface IAvailabilityRepository
{
    Task<IReadOnlyList<Recipe>> GetRecipesForProductAsync(Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Recipe>> GetRecipesForModifierOptionAsync(Guid modifierOptionId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, StockItem>> GetStockItemsByIdAsync(IReadOnlyCollection<Guid> stockItemIds, CancellationToken cancellationToken);
}
