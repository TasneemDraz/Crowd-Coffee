using CrowdCoffee.Application.Common;
using CrowdCoffee.Domain.Ordering;
using Microsoft.Extensions.Logging;

namespace CrowdCoffee.Application.Inventory;

/// <summary>Story 4.2: reacts to Epic 3's OrderCompletedEvent (Delivered/PickedUp) rather than
/// the state machine calling into inventory directly — same reasoning as
/// ReleaseStockOnOrderCancelledHandler for OrderCancelledEvent.</summary>
public class FulfillStockOnOrderCompletedHandler(StockReservationService reservationService, ILogger<FulfillStockOnOrderCompletedHandler> logger)
    : IDomainEventHandler<OrderCompletedEvent>
{
    public async Task HandleAsync(OrderCompletedEvent domainEvent, CancellationToken cancellationToken)
    {
        var result = await reservationService.FulfillForOrderAsync(domainEvent.OrderId, "system", DateTime.UtcNow, cancellationToken);
        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to fulfill stock reservations for completed order {OrderId}: {ErrorCode} {ErrorMessage}",
                domainEvent.OrderId, result.Error!.Code, result.Error.Message);
        }
    }
}
