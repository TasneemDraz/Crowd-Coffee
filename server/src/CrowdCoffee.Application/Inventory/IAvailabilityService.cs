using CrowdCoffee.Domain.Catalog;

namespace CrowdCoffee.Application.Inventory;

public interface IAvailabilityService
{
    /// Combines the manual override (Product.IsAvailable — the "we're out of oat milk today"
    /// switch, §8/§11) with the recipe-driven computed availability. Manual false always wins,
    /// enforced here so every caller gets the combination right rather than each having to
    /// remember to AND the two together itself.
    Task<bool> IsProductAvailableAsync(Product product, CancellationToken cancellationToken);

    Task<bool> IsModifierOptionAvailableAsync(ModifierOption modifierOption, CancellationToken cancellationToken);
}
