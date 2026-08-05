using CrowdCoffee.Application.Inventory;
using CrowdCoffee.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace CrowdCoffee.Infrastructure.Persistence.Repositories;

public class AvailabilityRepository(CrowdCoffeeDbContext dbContext) : IAvailabilityRepository
{
    public async Task<IReadOnlyList<Recipe>> GetRecipesForProductAsync(Guid productId, CancellationToken cancellationToken) =>
        await dbContext.Recipes.Where(r => r.ProductId == productId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Recipe>> GetRecipesForModifierOptionAsync(Guid modifierOptionId, CancellationToken cancellationToken) =>
        await dbContext.Recipes.Where(r => r.ModifierOptionId == modifierOptionId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, StockItem>> GetStockItemsByIdAsync(IReadOnlyCollection<Guid> stockItemIds, CancellationToken cancellationToken)
    {
        var stockItems = await dbContext.StockItems.Where(s => stockItemIds.Contains(s.Id)).ToListAsync(cancellationToken);
        return stockItems.ToDictionary(s => s.Id);
    }
}
