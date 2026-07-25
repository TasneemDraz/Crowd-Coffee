namespace CrowdCoffee.Domain.Inventory;

/// <summary>
/// Links a StockItem to whatever consumes it. Extended beyond the PRD §6 base shape per §4a
/// rule 5: modifiers consume ingredients too (oat milk deducts from the oat milk StockItem, an
/// extra shot deducts more beans), so a Recipe attaches to exactly one of Product or
/// ModifierOption, never both, never neither.
/// </summary>
public class Recipe
{
    public Guid Id { get; private set; }
    public Guid? ProductId { get; private set; }
    public Guid? ModifierOptionId { get; private set; }
    public Guid StockItemId { get; private set; }
    public decimal QuantityPerUnit { get; private set; }

    private Recipe()
    {
    }

    private Recipe(Guid id, Guid? productId, Guid? modifierOptionId, Guid stockItemId, decimal quantityPerUnit)
    {
        if (productId is null == modifierOptionId is null)
        {
            throw new InvalidOperationException("A Recipe must attach to exactly one of Product or ModifierOption.");
        }

        Id = id;
        ProductId = productId;
        ModifierOptionId = modifierOptionId;
        StockItemId = stockItemId;
        QuantityPerUnit = quantityPerUnit;
    }

    public static Recipe ForProduct(Guid id, Guid productId, Guid stockItemId, decimal quantityPerUnit) =>
        new(id, productId, null, stockItemId, quantityPerUnit);

    public static Recipe ForModifierOption(Guid id, Guid modifierOptionId, Guid stockItemId, decimal quantityPerUnit) =>
        new(id, null, modifierOptionId, stockItemId, quantityPerUnit);
}
