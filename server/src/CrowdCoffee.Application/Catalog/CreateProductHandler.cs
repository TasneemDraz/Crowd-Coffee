using CrowdCoffee.Application.Common;
using CrowdCoffee.Domain.Catalog;
using CrowdCoffee.Domain.Common;
using FluentValidation;

namespace CrowdCoffee.Application.Catalog;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(r => r.CategorySlug).NotEmpty();
        RuleFor(r => r.Slug).NotEmpty().Matches("^[a-z0-9]+(-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase letters, numbers, and hyphens only.");
        RuleFor(r => r.Name).NotEmpty();
        RuleFor(r => r.Price).GreaterThan(0);
        RuleFor(r => r.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateProductHandler(IAdminCatalogRepository repository, ICatalogCache cache)
{
    public async Task<Result<AdminProductDto>> HandleAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (!await repository.CategorySlugExistsAsync(request.CategorySlug, cancellationToken))
        {
            return Result.Failure<AdminProductDto>(Error.Validation(
                "Product.UnknownCategory", $"No category with slug '{request.CategorySlug}'."));
        }

        if (await repository.ProductSlugExistsAsync(request.Slug, cancellationToken))
        {
            return Result.Failure<AdminProductDto>(Error.Conflict(
                "Product.SlugExists", $"A product with slug '{request.Slug}' already exists."));
        }

        var product = new Product(
            Guid.NewGuid(), request.CategorySlug, request.Slug, request.Name, request.Description,
            request.Price, request.DisplayOrder, request.ImageUrl);

        repository.AddProduct(product);
        await repository.SaveChangesAsync(cancellationToken);
        cache.InvalidateAll();

        return Result.Success(new AdminProductDto(
            product.Id, product.CategorySlug, product.Slug, product.Name, product.Description,
            product.Price, product.ImageUrl, product.DisplayOrder, product.IsAvailable, product.IsActive));
    }
}
