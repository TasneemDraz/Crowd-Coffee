using CrowdCoffee.Domain.Common;
using CrowdCoffee.Domain.Inventory;
using FluentValidation;

namespace CrowdCoffee.Application.Inventory;

public record RecordPurchaseRequest(Guid StockItemId, decimal Quantity, string CreatedBy, string? Notes);

public class RecordPurchaseRequestValidator : AbstractValidator<RecordPurchaseRequest>
{
    public RecordPurchaseRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Purchase quantity must be positive.");
        RuleFor(x => x.CreatedBy).NotEmpty();
    }
}

public class RecordPurchaseHandler(IStockRepository repository)
{
    public async Task<Result> HandleAsync(RecordPurchaseRequest request, DateTime occurredAtUtc, CancellationToken cancellationToken)
    {
        var stockItem = await repository.GetByIdAsync(request.StockItemId, cancellationToken);
        if (stockItem is null)
        {
            return Result.Failure(Error.NotFound("StockItem.NotFound", $"StockItem {request.StockItemId} not found."));
        }

        var adjustResult = stockItem.AdjustOnHand(request.Quantity);
        if (adjustResult.IsFailure)
        {
            return adjustResult;
        }

        repository.AddStockMovement(new StockMovement(
            Guid.NewGuid(), stockItem.Id, request.Quantity, StockMovementType.Purchase, request.CreatedBy, occurredAtUtc,
            notes: request.Notes));

        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
