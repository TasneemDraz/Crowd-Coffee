using CrowdCoffee.Domain.Common;

namespace CrowdCoffee.Application.Inventory;

public class GetStockMovementsHandler(IStockRepository repository)
{
    public async Task<Result<IReadOnlyList<StockMovementDto>>> HandleAsync(Guid stockItemId, CancellationToken cancellationToken)
    {
        var stockItem = await repository.GetByIdAsync(stockItemId, cancellationToken);
        if (stockItem is null)
        {
            return Result.Failure<IReadOnlyList<StockMovementDto>>(
                Error.NotFound("StockItem.NotFound", $"StockItem {stockItemId} not found."));
        }

        var movements = await repository.GetMovementsForStockItemAsync(stockItemId, take: 200, cancellationToken);
        IReadOnlyList<StockMovementDto> dtos = movements
            .Select(m => new StockMovementDto(
                m.Id, m.Quantity, m.MovementType.ToString(), m.ReferenceType, m.ReferenceId, m.Notes, m.CreatedBy, m.CreatedAt))
            .ToList();

        return Result.Success(dtos);
    }
}
