namespace CrowdCoffee.Domain.Ordering;

public class OrderItemModifier
{
    public Guid Id { get; private set; }
    public Guid OrderItemId { get; private set; }
    public Guid ModifierOptionId { get; private set; }

    /// Snapshot — same reasoning as OrderItem.ProductName (§4a rule 1).
    public string ModifierName { get; private set; } = string.Empty;
    public decimal PriceDelta { get; private set; }

    private OrderItemModifier()
    {
    }

    public OrderItemModifier(Guid id, Guid orderItemId, Guid modifierOptionId, string modifierName, decimal priceDelta)
    {
        Id = id;
        OrderItemId = orderItemId;
        ModifierOptionId = modifierOptionId;
        ModifierName = modifierName;
        PriceDelta = priceDelta;
    }
}
