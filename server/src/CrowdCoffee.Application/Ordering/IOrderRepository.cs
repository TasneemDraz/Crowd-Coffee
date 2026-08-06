using CrowdCoffee.Domain.Ordering;

namespace CrowdCoffee.Application.Ordering;

public interface IOrderRepository
{
    /// Story 9.3: idempotency. Loaded with Items/Modifiers so a repeat-key response can return
    /// the full order shape without a second round-trip.
    Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);

    /// Story 10.5's timeout job: PendingPayment orders older than the cutoff, whose customers
    /// abandoned checkout before the gateway ever confirmed.
    Task<IReadOnlyList<Order>> GetStalePendingPaymentOrdersAsync(DateTime olderThanUtc, CancellationToken cancellationToken);

    /// Epic 12's kitchen board: orders currently on it (Confirmed/Preparing/Ready), oldest first
    /// so the board reads as a FIFO queue.
    Task<IReadOnlyList<Order>> GetActiveKitchenOrdersAsync(CancellationToken cancellationToken);

    /// Story 13.4's order-history screen. AsNoTracking, no Items include — Order.Total is a
    /// stored, precomputed column (Epic 9), so the summary list needs no join at all.
    Task<IReadOnlyList<Order>> SearchOrdersAsync(OrderSearchFilters filters, CancellationToken cancellationToken);

    /// Separate from GetByIdAsync (used by the checkout-return poll and the idempotency check —
    /// both hot-ish paths that don't need status history) so those callers don't pay for an extra
    /// join they never read.
    Task<Order?> GetByIdWithHistoryAsync(Guid orderId, CancellationToken cancellationToken);

    void Add(Order order);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
