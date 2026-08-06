namespace CrowdCoffee.Application.Catalog;

public class AdminListProductsHandler(IAdminCatalogRepository repository)
{
    public Task<IReadOnlyList<AdminProductDto>> HandleAsync(CancellationToken cancellationToken) =>
        repository.GetAllProductsAsync(cancellationToken);
}
