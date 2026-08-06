using CrowdCoffee.Application.Common;
using CrowdCoffee.Domain.Common;
using FluentValidation;

namespace CrowdCoffee.Application.Catalog;

public class UpdateModifierOptionRequestValidator : AbstractValidator<UpdateModifierOptionRequest>
{
    public UpdateModifierOptionRequestValidator()
    {
        RuleFor(r => r.PriceDelta).GreaterThanOrEqualTo(0);
    }
}

/// AC (Story 13.2): edit price deltas/active state "without rewriting historical snapshots" —
/// satisfied structurally, not by anything this handler does: OrderItemModifier already snapshots
/// ModifierName/PriceDelta at order-creation time (Epic 2.2/9.2), so mutating the live
/// ModifierOption here never touches a single past order.
public class UpdateModifierOptionHandler(IAdminCatalogRepository repository, ICatalogCache cache)
{
    public async Task<Result<AdminModifierOptionDto>> HandleAsync(Guid optionId, UpdateModifierOptionRequest request, CancellationToken cancellationToken)
    {
        var option = await repository.GetModifierOptionAsync(optionId, cancellationToken);
        if (option is null)
        {
            return Result.Failure<AdminModifierOptionDto>(Error.NotFound("ModifierOption.NotFound", $"No modifier option with id '{optionId}'."));
        }

        option.UpdatePriceDelta(request.PriceDelta);
        option.SetActive(request.IsActive);

        await repository.SaveChangesAsync(cancellationToken);
        cache.InvalidateAll();

        return Result.Success(new AdminModifierOptionDto(option.Id, option.Slug, option.Name, option.PriceDelta, option.DisplayOrder, option.IsActive));
    }
}
