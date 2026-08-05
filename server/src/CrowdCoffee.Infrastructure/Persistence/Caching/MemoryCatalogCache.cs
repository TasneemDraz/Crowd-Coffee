using CrowdCoffee.Application.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace CrowdCoffee.Infrastructure.Persistence.Caching;

/// <summary>
/// IMemoryCache has no built-in "clear everything" — the standard workaround is a shared
/// CancellationTokenSource that every entry links an expiration token to, swapped out (and the
/// old one cancelled) on <see cref="InvalidateAll"/>. Registered as a singleton: the whole point
/// is one shared reset token across every request, not a per-scope one.
/// </summary>
public class MemoryCatalogCache(IMemoryCache cache) : ICatalogCache
{
    // Backstops the (currently nonexistent) Epic 13 write path — explicit invalidation is the
    // real mechanism; this just bounds worst-case staleness in the meantime.
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    private CancellationTokenSource _resetSource = new();

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var value = await factory(cancellationToken);

        using var entry = cache.CreateEntry(key);
        entry.Value = value;
        entry.AbsoluteExpirationRelativeToNow = Ttl;
        entry.ExpirationTokens.Add(new CancellationChangeToken(_resetSource.Token));

        return value;
    }

    public void InvalidateAll()
    {
        var previous = Interlocked.Exchange(ref _resetSource, new CancellationTokenSource());
        previous.Cancel();
        previous.Dispose();
    }
}
