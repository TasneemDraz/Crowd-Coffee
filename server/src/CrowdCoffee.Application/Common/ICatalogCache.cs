namespace CrowdCoffee.Application.Common;

/// <summary>
/// Caches Epic 5's read-model queries (categories/products/modifier groups) so repeat menu
/// requests skip the DB roundtrip entirely, per §12's &lt;200ms budget. Deliberately does NOT
/// cache computed availability (Epic 4) — that's recomputed from live stock on every call, so a
/// cached response can never show something in stock that isn't, or vice versa (see
/// GetProductsHandler/GetProductModifiersHandler). "Writes invalidate cache promptly" (Story 5.3
/// AC) means <see cref="InvalidateAll"/>, called from Epic 13's future admin product/category/
/// modifier write endpoints — nothing calls it yet since Epic 13 doesn't exist. A short TTL
/// backstops that gap in the meantime; explicit invalidation is still the primary mechanism, not
/// TTL convenience.
/// </summary>
public interface ICatalogCache
{
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken);

    void InvalidateAll();
}
