using CrowdCoffee.Application.Common;

namespace CrowdCoffee.Application.Catalog;

/// <summary>
/// §8: the 19 seeded categories in display order. No availability concept applies to a
/// category itself (only products/modifier options can be unavailable), so this is a straight
/// cached passthrough over the repository.
/// </summary>
public class GetCategoriesHandler(ICatalogRepository repository, ICatalogCache cache)
{
    public Task<IReadOnlyList<CategoryDto>> HandleAsync(CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(CatalogCacheKeys.Categories, repository.GetActiveCategoriesAsync, cancellationToken);
}
