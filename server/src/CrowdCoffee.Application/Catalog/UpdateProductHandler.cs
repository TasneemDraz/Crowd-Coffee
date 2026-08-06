using CrowdCoffee.Application.Common;
using CrowdCoffee.Domain.Common;
using FluentValidation;

namespace CrowdCoffee.Application.Catalog;

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty();
        RuleFor(r => r.Price).GreaterThan(0);
        RuleFor(r => r.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateProductHandler(IAdminCatalogRepository repository, ICatalogCache cache)
{
    public async Task<Result<AdminProductDto>> HandleAsync(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await repository.GetProductAsync(productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<AdminProductDto>(Error.NotFound("Product.NotFound", $"No product with id '{productId}'."));
        }

        product.UpdateDetails(request.Name, request.Description, request.ImageUrl);
        product.UpdatePrice(request.Price);
        product.SetDisplayOrder(request.DisplayOrder);
        product.SetManualAvailability(request.IsAvailable);

        if (request.IsActive)
        {
            product.Activate();
        }
        else
        {
            product.Deactivate();
        }

        await repository.SaveChangesAsync(cancellationToken);
        cache.InvalidateAll();

        return Result.Success(new AdminProductDto(
            product.Id, product.CategorySlug, product.Slug, product.Name, product.Description,
            product.Price, product.ImageUrl, product.DisplayOrder, product.IsAvailable, product.IsActive));
    }
}
