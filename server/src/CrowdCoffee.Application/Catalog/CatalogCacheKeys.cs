namespace CrowdCoffee.Application.Catalog;

public static class CatalogCacheKeys
{
    public const string Categories = "catalog:categories";
    public const string Products = "catalog:products";

    public static string ProductBySlug(string slug) => $"catalog:product:{slug}";

    public static string ModifierGroupsForCategory(string categorySlug) => $"catalog:modifier-groups:{categorySlug}";
}
