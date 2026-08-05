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

    /// Computed-only (recipe vs. stock) availability for many products in two queries total,
    /// instead of the single-item overload's per-product query pair — Epic 5's catalog list
    /// needs this to stay inside the &lt;200ms budget across the full menu. Unlike the
    /// single-item overload, this does NOT know about Product.IsAvailable (it takes ids, not
    /// entities) — callers still owe the same manual-override AND themselves. Every requested id
    /// is present as a key in the result, including ids with zero recipe rows (mapped to `true`,
    /// same as AvailabilityCalculator's zero-recipes rule).
    Task<IReadOnlyDictionary<Guid, bool>> GetComputedAvailabilityForProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, bool>> GetComputedAvailabilityForModifierOptionsAsync(IReadOnlyCollection<Guid> modifierOptionIds, CancellationToken cancellationToken);
}
