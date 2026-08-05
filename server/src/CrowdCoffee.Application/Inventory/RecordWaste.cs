using CrowdCoffee.Domain.Common;
using CrowdCoffee.Domain.Inventory;
using FluentValidation;

namespace CrowdCoffee.Application.Inventory;

public record RecordWasteRequest(Guid StockItemId, decimal Quantity, string CreatedBy, string Reason);

public class RecordWasteRequestValidator : AbstractValidator<RecordWasteRequest>
{
    public RecordWasteRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Waste quantity must be positive.");
        RuleFor(x => x.CreatedBy).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A reason is required to record waste.");
    }
}

public class RecordWasteHandler(IStockRepository repository)
{
    public async Task<Result> HandleAsync(RecordWasteRequest request, DateTime occurredAtUtc, CancellationToken cancellationToken)
    {
        var stockItem = await repository.GetByIdAsync(request.StockItemId, cancellationToken);
        if (stockItem is null)
        {
            return Result.Failure(Error.NotFound("StockItem.NotFound", $"StockItem {request.StockItemId} not found."));
        }

        var adjustResult = stockItem.AdjustOnHand(-request.Quantity);
        if (adjustResult.IsFailure)
        {
            return adjustResult;
        }

        repository.AddStockMovement(new StockMovement(
            Guid.NewGuid(), stockItem.Id, -request.Quantity, StockMovementType.Waste, request.CreatedBy, occurredAtUtc,
            notes: request.Reason));

        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
