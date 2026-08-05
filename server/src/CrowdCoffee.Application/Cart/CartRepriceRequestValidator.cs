using FluentValidation;

namespace CrowdCoffee.Application.Cart;

public class CartRepriceRequestValidator : AbstractValidator<CartRepriceRequest>
{
    public CartRepriceRequestValidator()
    {
        RuleFor(r => r.Lines).NotEmpty();
        RuleForEach(r => r.Lines).SetValidator(new CartLineRepriceRequestValidator());
    }
}

public class CartLineRepriceRequestValidator : AbstractValidator<CartLineRepriceRequest>
{
    public CartLineRepriceRequestValidator()
    {
        RuleFor(l => l.ProductId).NotEmpty();
        RuleFor(l => l.MilkOptionId).NotEmpty();
        RuleFor(l => l.SyrupOptionIds).Must(ids => ids.Count <= 2).WithMessage("At most 2 syrups are allowed.");
        RuleFor(l => l.Quantity).GreaterThan(0);
    }
}
