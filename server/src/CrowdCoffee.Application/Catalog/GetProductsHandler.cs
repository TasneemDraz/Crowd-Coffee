using CrowdCoffee.Application.Common;
using CrowdCoffee.Application.Inventory;

namespace CrowdCoffee.Application.Catalog;

/// <summary>
/// §8: products filtered to IsActive (query-time) && IsAvailable, where IsAvailable is the
/// manual override (Product.IsAvailable, cached structure) ANDed with Epic 4's computed
/// (recipe-vs-stock) availability, which is always recomputed live — never cached — so this
/// list can never show something out of stock.
/// </summary>
public class GetProductsHandler(ICatalogRepository repository, IAvailabilityService availabilityService, ICatalogCache cache)
{
    public async Task<IReadOnlyList<ProductDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var rows = await cache.GetOrCreateAsync(CatalogCacheKeys.Products, repository.GetActiveProductsAsync, cancellationToken);

        var computed = await availabilityService.GetComputedAvailabilityForProductsAsync(
            rows.Select(r => r.Id).ToList(), cancellationToken);

        return rows
            .Select(r => new ProductDto(
                r.Id, r.CategorySlug, r.Slug, r.Name, r.Description, r.Price, r.ImageUrl, r.DisplayOrder,
                IsAvailable: r.IsManuallyAvailable && computed.GetValueOrDefault(r.Id, true)))
            .Where(p => p.IsAvailable)
            .ToList();
    }
}
