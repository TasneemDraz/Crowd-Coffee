using CrowdCoffee.Domain.Inventory;

namespace CrowdCoffee.Application.Inventory;

public interface IAvailabilityRepository
{
    Task<IReadOnlyList<Recipe>> GetRecipesForProductAsync(Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Recipe>> GetRecipesForModifierOptionAsync(Guid modifierOptionId, CancellationToken cancellationToken);

    /// Bulk sibling of <see cref="GetRecipesForProductAsync"/> — one query for many products,
    /// instead of one query per product, so Epic 5's catalog list stays inside the &lt;200ms
    /// budget regardless of menu size.
    Task<IReadOnlyList<Recipe>> GetRecipesForProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<Recipe>> GetRecipesForModifierOptionsAsync(IReadOnlyCollection<Guid> modifierOptionIds, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, StockItem>> GetStockItemsByIdAsync(IReadOnlyCollection<Guid> stockItemIds, CancellationToken cancellationToken);
}
