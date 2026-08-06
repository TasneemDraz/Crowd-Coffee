using CrowdCoffee.Domain.Ordering;
using FluentValidation;

namespace CrowdCoffee.Application.Ordering;

/// <summary>
/// §12: name/phone always required, address required for delivery (§9.1's AC). Deeper
/// per-line modifier validation (does this option genuinely belong to this product) needs live
/// DB data and happens in CartLineRepricer via CreateOrderHandler, not here — this layer only
/// catches shape violations before the handler runs, same split as CartRepriceRequestValidator.
/// </summary>
public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(r => r.CustomerName).NotEmpty();
        RuleFor(r => r.Phone).NotEmpty();
        RuleFor(r => r.IdempotencyKey).NotEmpty();
        RuleFor(r => r.Address).NotEmpty().When(r => r.Mode == OrderMode.Delivery)
            .WithMessage("Address is required for delivery orders.");
        RuleFor(r => r.Lines).NotEmpty();
        RuleForEach(r => r.Lines).SetValidator(new CreateOrderLineRequestValidator());
    }
}

public class CreateOrderLineRequestValidator : AbstractValidator<CreateOrderLineRequest>
{
    public CreateOrderLineRequestValidator()
    {
        RuleFor(l => l.ProductId).NotEmpty();
        RuleFor(l => l.SyrupOptionIds).Must(ids => ids.Count <= 2).WithMessage("At most 2 syrups are allowed.");
        RuleFor(l => l.Quantity).GreaterThan(0);
    }
}
