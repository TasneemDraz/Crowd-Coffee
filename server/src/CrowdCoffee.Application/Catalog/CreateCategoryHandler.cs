using CrowdCoffee.Application.Common;
using CrowdCoffee.Domain.Catalog;
using CrowdCoffee.Domain.Common;
using FluentValidation;

namespace CrowdCoffee.Application.Catalog;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(r => r.Slug).NotEmpty().Matches("^[a-z0-9]+(-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase letters, numbers, and hyphens only.");
        RuleFor(r => r.Label).NotEmpty();
        RuleFor(r => r.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateCategoryHandler(IAdminCatalogRepository repository, ICatalogCache cache)
{
    public async Task<Result<AdminCategoryDto>> HandleAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        if (await repository.CategorySlugExistsAsync(request.Slug, cancellationToken))
        {
            return Result.Failure<AdminCategoryDto>(Error.Conflict(
                "Category.SlugExists", $"A category with slug '{request.Slug}' already exists."));
        }

        var category = new Category(Guid.NewGuid(), request.Slug, request.Label, request.Description, request.DisplayOrder);
        repository.AddCategory(category);
        await repository.SaveChangesAsync(cancellationToken);
        cache.InvalidateAll();

        return Result.Success(new AdminCategoryDto(
            category.Id, category.Slug, category.Label, category.Description, category.DisplayOrder, category.IsActive));
    }
}
