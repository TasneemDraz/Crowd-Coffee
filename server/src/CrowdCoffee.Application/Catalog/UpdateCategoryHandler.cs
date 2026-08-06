using CrowdCoffee.Application.Common;
using CrowdCoffee.Domain.Common;
using FluentValidation;

namespace CrowdCoffee.Application.Catalog;

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(r => r.Label).NotEmpty();
        RuleFor(r => r.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateCategoryHandler(IAdminCatalogRepository repository, ICatalogCache cache)
{
    public async Task<Result<AdminCategoryDto>> HandleAsync(Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await repository.GetCategoryAsync(categoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure<AdminCategoryDto>(Error.NotFound("Category.NotFound", $"No category with id '{categoryId}'."));
        }

        category.Update(request.Label, request.Description, request.DisplayOrder);
        category.SetActive(request.IsActive);

        await repository.SaveChangesAsync(cancellationToken);
        cache.InvalidateAll();

        return Result.Success(new AdminCategoryDto(
            category.Id, category.Slug, category.Label, category.Description, category.DisplayOrder, category.IsActive));
    }
}
