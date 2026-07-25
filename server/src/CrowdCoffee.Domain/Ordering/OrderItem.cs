namespace CrowdCoffee.Domain.Ordering;

public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }

    /// Snapshot — a later price/name change must not rewrite this order (§6 rule 1).
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }
    public decimal LineTotal { get; private set; }

    private readonly List<OrderItemModifier> _modifiers = [];
    public IReadOnlyCollection<OrderItemModifier> Modifiers => _modifiers;

    private OrderItem()
    {
    }

    public OrderItem(Guid id, Guid orderId, Guid productId, string productName, decimal unitPrice, int quantity)
    {
        Id = id;
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        LineTotal = unitPrice * quantity;
    }

    public void AddModifier(OrderItemModifier modifier) => _modifiers.Add(modifier);
}
