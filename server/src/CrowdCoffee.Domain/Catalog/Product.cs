namespace CrowdCoffee.Domain.Catalog;

public class Product
{
    public Guid Id { get; private set; }
    public string CategorySlug { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string? ImageUrl { get; private set; }

    /// Manager's manual override ("we're out of oat milk today", §8/§11). Combined with
    /// recipe-driven computed availability (Epic 4) — manual false always wins.
    public bool IsAvailable { get; private set; } = true;
    public bool IsActive { get; private set; } = true;
    public int DisplayOrder { get; private set; }

    private Product()
    {
    }

    public Product(Guid id, string categorySlug, string slug, string name, string? description, decimal price, int displayOrder, string? imageUrl = null)
    {
        Id = id;
        CategorySlug = categorySlug;
        Slug = slug;
        Name = name;
        Description = description;
        Price = price;
        DisplayOrder = displayOrder;
        ImageUrl = imageUrl;
        IsAvailable = true;
        IsActive = true;
    }

    public void SetManualAvailability(bool isAvailable) => IsAvailable = isAvailable;

    public void UpdatePrice(decimal price) => Price = price;

    public void UpdateDetails(string name, string? description, string? imageUrl)
    {
        Name = name;
        Description = description;
        ImageUrl = imageUrl;
    }

    public void SetDisplayOrder(int displayOrder) => DisplayOrder = displayOrder;

    public void Deactivate() => IsActive = false;
}
