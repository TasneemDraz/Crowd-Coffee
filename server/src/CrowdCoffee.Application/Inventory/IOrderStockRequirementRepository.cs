using CrowdCoffee.Domain.Ordering;

namespace CrowdCoffee.Application.Inventory;

public interface IOrderStockRequirementRepository
{
    /// Loads the order with Items and each item's Modifiers — the full graph the requirement
    /// computation needs. Callers must not rely on any other order this repository might have
    /// loaded with a shallower Include.
    Task<Order?> GetOrderWithItemsAsync(Guid orderId, CancellationToken cancellationToken);

    /// Aggregates every ingredient a given order needs across its items' base-product recipes
    /// and their modifiers' recipes (§4a rule 5 — a StockItem needed by both nets into one
    /// entry, e.g. milk used by the base drink and by an "extra milk" modifier).
    Task<IReadOnlyDictionary<Guid, decimal>> GetRequiredQuantitiesAsync(Order order, CancellationToken cancellationToken);
}
