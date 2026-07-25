namespace CrowdCoffee.Domain.Catalog;

/// <summary>
/// Maps which modifier groups apply to a category of products. Category-wide for v1 per §4a's
/// DRINK_CATS rule (all drink categories share the same milk/shot/syrup groups) — simpler than a
/// per-product join, per the PRD's stated v1 simplification.
/// </summary>
public class ProductModifierGroup
{
    public Guid Id { get; private set; }
    public string CategorySlug { get; private set; } = string.Empty;
    public Guid ModifierGroupId { get; private set; }

    private ProductModifierGroup()
    {
    }

    public ProductModifierGroup(Guid id, string categorySlug, Guid modifierGroupId)
    {
        Id = id;
        CategorySlug = categorySlug;
        ModifierGroupId = modifierGroupId;
    }
}
