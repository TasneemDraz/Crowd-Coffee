namespace CrowdCoffee.Domain.Catalog;

public class ModifierOption
{
    public Guid Id { get; private set; }
    public Guid ModifierGroupId { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal PriceDelta { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int DisplayOrder { get; private set; }

    private ModifierOption()
    {
    }

    public ModifierOption(Guid id, Guid modifierGroupId, string slug, string name, decimal priceDelta, int displayOrder)
    {
        Id = id;
        ModifierGroupId = modifierGroupId;
        Slug = slug;
        Name = name;
        PriceDelta = priceDelta;
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public void UpdatePriceDelta(decimal priceDelta) => PriceDelta = priceDelta;

    public void SetActive(bool isActive) => IsActive = isActive;
}
