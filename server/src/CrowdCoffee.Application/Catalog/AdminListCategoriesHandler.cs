namespace CrowdCoffee.Application.Catalog;

/// Unlike GetCategoriesHandler (Epic 5, cached, active-only), this deliberately bypasses
/// ICatalogCache — admin needs to see inactive rows too, and this isn't a <200ms-budget hot path.
public class AdminListCategoriesHandler(IAdminCatalogRepository repository)
{
    public Task<IReadOnlyList<AdminCategoryDto>> HandleAsync(CancellationToken cancellationToken) =>
        repository.GetAllCategoriesAsync(cancellationToken);
}
