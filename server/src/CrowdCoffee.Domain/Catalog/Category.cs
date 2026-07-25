namespace CrowdCoffee.Domain.Catalog;

public class Category
{
    public Guid Id { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Category()
    {
    }

    public Category(Guid id, string slug, string label, string? description, int displayOrder)
    {
        Id = id;
        Slug = slug;
        Label = label;
        Description = description;
        DisplayOrder = displayOrder;
        IsActive = true;
    }
}
