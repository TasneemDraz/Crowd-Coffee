using CrowdCoffee.Domain.Common;
using CrowdCoffee.Domain.Inventory;
using FluentValidation;

namespace CrowdCoffee.Application.Inventory;

/// QuantityDelta is signed — an adjustment can correct in either direction (found more stock
/// than expected, or less), unlike Purchase/Waste which are each inherently one-directional.
public record RecordAdjustmentRequest(Guid StockItemId, decimal QuantityDelta, string CreatedBy, string Reason);

public class RecordAdjustmentRequestValidator : AbstractValidator<RecordAdjustmentRequest>
{
    public RecordAdjustmentRequestValidator()
    {
        RuleFor(x => x.QuantityDelta).NotEqual(0).WithMessage("An adjustment must change the quantity by a non-zero amount.");
        RuleFor(x => x.CreatedBy).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A reason is required to record an adjustment.");
    }
}

public class RecordAdjustmentHandler(IStockRepository repository)
{
    public async Task<Result> HandleAsync(RecordAdjustmentRequest request, DateTime occurredAtUtc, CancellationToken cancellationToken)
    {
        var stockItem = await repository.GetByIdAsync(request.StockItemId, cancellationToken);
        if (stockItem is null)
        {
            return Result.Failure(Error.NotFound("StockItem.NotFound", $"StockItem {request.StockItemId} not found."));
        }

        var adjustResult = stockItem.AdjustOnHand(request.QuantityDelta);
        if (adjustResult.IsFailure)
        {
            return adjustResult;
        }

        repository.AddStockMovement(new StockMovement(
            Guid.NewGuid(), stockItem.Id, request.QuantityDelta, StockMovementType.Adjustment, request.CreatedBy, occurredAtUtc,
            notes: request.Reason));

        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
