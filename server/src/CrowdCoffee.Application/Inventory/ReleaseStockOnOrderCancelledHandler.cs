using CrowdCoffee.Application.Common;
using CrowdCoffee.Domain.Ordering;
using Microsoft.Extensions.Logging;

namespace CrowdCoffee.Application.Inventory;

/// <summary>§8: cancellation releases stock reservations. Reacts to Epic 3's OrderCancelledEvent
/// rather than the state machine calling into inventory directly — Order still doesn't know
/// StockItem exists.</summary>
public class ReleaseStockOnOrderCancelledHandler(StockReservationService reservationService, ILogger<ReleaseStockOnOrderCancelledHandler> logger)
    : IDomainEventHandler<OrderCancelledEvent>
{
    public async Task HandleAsync(OrderCancelledEvent domainEvent, CancellationToken cancellationToken)
    {
        var result = await reservationService.ReleaseForOrderAsync(domainEvent.OrderId, cancellationToken);
        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to release stock reservations for cancelled order {OrderId}: {ErrorCode} {ErrorMessage}",
                domainEvent.OrderId, result.Error!.Code, result.Error.Message);
        }
    }
}
