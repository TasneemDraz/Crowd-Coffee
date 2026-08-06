namespace CrowdCoffee.Application.Catalog;

public class AdminListModifierGroupsHandler(IAdminCatalogRepository repository)
{
    public async Task<IReadOnlyList<AdminModifierGroupDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var groups = await repository.GetAllModifierGroupsAsync(cancellationToken);
        return groups
            .Select(g => new AdminModifierGroupDto(
                g.Id, g.Slug, g.Name, g.SelectionType.ToString(), g.MaxSelections, g.IsRequired, g.DisplayOrder,
                g.Options
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => new AdminModifierOptionDto(o.Id, o.Slug, o.Name, o.PriceDelta, o.DisplayOrder, o.IsActive))
                    .ToList()))
            .ToList();
    }
}
