using CrowdCoffee.Application.Inventory;
using CrowdCoffee.Domain.Inventory;
using CrowdCoffee.Domain.Ordering;
using Microsoft.EntityFrameworkCore;

namespace CrowdCoffee.Infrastructure.Persistence.Repositories;

public class OrderStockRequirementRepository(CrowdCoffeeDbContext dbContext) : IOrderStockRequirementRepository
{
    public async Task<Order?> GetOrderWithItemsAsync(Guid orderId, CancellationToken cancellationToken) =>
        await dbContext.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Modifiers)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetRequiredQuantitiesAsync(Order order, CancellationToken cancellationToken)
    {
        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var modifierOptionIds = order.Items.SelectMany(i => i.Modifiers).Select(m => m.ModifierOptionId).Distinct().ToList();

        List<Recipe> productRecipes = productIds.Count == 0
            ? []
            : await dbContext.Recipes
                .Where(r => r.ProductId != null && productIds.Contains(r.ProductId.Value))
                .ToListAsync(cancellationToken);

        List<Recipe> modifierRecipes = modifierOptionIds.Count == 0
            ? []
            : await dbContext.Recipes
                .Where(r => r.ModifierOptionId != null && modifierOptionIds.Contains(r.ModifierOptionId.Value))
                .ToListAsync(cancellationToken);

        var requirements = new Dictionary<Guid, decimal>();

        foreach (var item in order.Items)
        {
            foreach (var recipe in productRecipes.Where(r => r.ProductId == item.ProductId))
            {
                Accumulate(requirements, recipe.StockItemId, recipe.QuantityPerUnit * item.Quantity);
            }

            foreach (var modifier in item.Modifiers)
            {
                foreach (var recipe in modifierRecipes.Where(r => r.ModifierOptionId == modifier.ModifierOptionId))
                {
                    Accumulate(requirements, recipe.StockItemId, recipe.QuantityPerUnit * item.Quantity);
                }
            }
        }

        return requirements;
    }

    private static void Accumulate(Dictionary<Guid, decimal> requirements, Guid stockItemId, decimal amount) =>
        requirements[stockItemId] = requirements.GetValueOrDefault(stockItemId) + amount;
}
