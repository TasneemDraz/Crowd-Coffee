using CrowdCoffee.Application.Common;
using CrowdCoffee.Application.Inventory;
using CrowdCoffee.Domain.Common;

namespace CrowdCoffee.Application.Catalog;

/// <summary>
/// §4a: modifier groups/options applicable to a product's category (DRINK_CATS), each option
/// flagged available/unavailable rather than filtered out entirely — unlike products (Story
/// 5.1), an out-of-stock milk should still show in the modal as disabled, not vanish, since the
/// UI needs a stable set of choices to render around.
/// </summary>
public class GetProductModifiersHandler(ICatalogRepository repository, IAvailabilityService availabilityService, ICatalogCache cache)
{
    public async Task<Result<IReadOnlyList<ModifierGroupDto>>> HandleAsync(string productSlug, CancellationToken cancellationToken)
    {
        var product = await cache.GetOrCreateAsync(
            CatalogCacheKeys.ProductBySlug(productSlug),
            ct => repository.GetActiveProductBySlugAsync(productSlug, ct),
            cancellationToken);

        if (product is null)
        {
            return Result.Failure<IReadOnlyList<ModifierGroupDto>>(
                Error.NotFound("Product.NotFound", $"No active product with slug '{productSlug}'."));
        }

        var groups = await cache.GetOrCreateAsync(
            CatalogCacheKeys.ModifierGroupsForCategory(product.CategorySlug),
            ct => repository.GetModifierGroupsForCategoryAsync(product.CategorySlug, ct),
            cancellationToken);

        var optionIds = groups.SelectMany(g => g.Options).Select(o => o.Id).ToList();
        var computed = await availabilityService.GetComputedAvailabilityForModifierOptionsAsync(optionIds, cancellationToken);

        var dtos = groups
            .Select(g => new ModifierGroupDto(
                g.Id, g.Slug, g.Name, g.SelectionType.ToString(), g.MaxSelections, g.IsRequired, g.DisplayOrder,
                g.Options
                    .Select(o => new ModifierOptionDto(
                        o.Id, o.Slug, o.Name, o.PriceDelta, o.DisplayOrder,
                        IsAvailable: o.IsManuallyActive && computed.GetValueOrDefault(o.Id, true)))
                    .ToList()))
            .ToList();

        return Result.Success<IReadOnlyList<ModifierGroupDto>>(dtos);
    }
}
