namespace CrowdCoffee.Application.Common;

/// <summary>
/// Application-layer seam over EF Core's transaction API (Infrastructure owns the real
/// DbContext — Application can't reference it directly, per the one-way layering in
/// ARCHITECTURE.md). Story 9.2 needs an explicit transaction spanning order-number generation,
/// order/item persistence, and — for cash orders — the immediate auto-confirm + stock
/// reservation, so a stock failure rolls back the order that was about to be created too.
/// </summary>
public interface IUnitOfWork
{
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);

    Task RollbackAsync(CancellationToken cancellationToken);
}
