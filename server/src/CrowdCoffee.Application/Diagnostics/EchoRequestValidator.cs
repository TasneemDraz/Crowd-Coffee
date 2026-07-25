using FluentValidation;

namespace CrowdCoffee.Application.Diagnostics;

public class EchoRequestValidator : AbstractValidator<EchoRequest>
{
    public EchoRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(200).WithMessage("Message must be 200 characters or fewer.");
    }
}
