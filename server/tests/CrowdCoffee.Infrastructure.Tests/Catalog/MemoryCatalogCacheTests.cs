using CrowdCoffee.Application.Common;
using CrowdCoffee.Infrastructure.Persistence.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace CrowdCoffee.Infrastructure.Tests.Catalog;

/// <summary>
/// No database needed — proves the cache mechanics themselves: repeat calls skip the factory,
/// and InvalidateAll() (the hook Epic 13's future admin writes will call) actually forces a
/// fresh factory call rather than serving stale data indefinitely.
/// </summary>
public class MemoryCatalogCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_SecondCallForSameKey_SkipsFactory()
    {
        var cache = new MemoryCatalogCache(new MemoryCache(new MemoryCacheOptions()));
        var factoryCalls = 0;

        Task<int> Factory(CancellationToken ct)
        {
            factoryCalls++;
            return Task.FromResult(42);
        }

        var first = await cache.GetOrCreateAsync("key", Factory, CancellationToken.None);
        var second = await cache.GetOrCreateAsync("key", Factory, CancellationToken.None);

        Assert.Equal(42, first);
        Assert.Equal(42, second);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task InvalidateAll_ForcesNextCallToRunFactoryAgain()
    {
        ICatalogCache cache = new MemoryCatalogCache(new MemoryCache(new MemoryCacheOptions()));
        var factoryCalls = 0;

        Task<int> Factory(CancellationToken ct)
        {
            factoryCalls++;
            return Task.FromResult(factoryCalls);
        }

        var first = await cache.GetOrCreateAsync("key", Factory, CancellationToken.None);
        cache.InvalidateAll();
        var second = await cache.GetOrCreateAsync("key", Factory, CancellationToken.None);

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(2, factoryCalls);
    }

    [Fact]
    public async Task InvalidateAll_ClearsEveryKey_NotJustOne()
    {
        ICatalogCache cache = new MemoryCatalogCache(new MemoryCache(new MemoryCacheOptions()));

        await cache.GetOrCreateAsync("a", _ => Task.FromResult(1), CancellationToken.None);
        await cache.GetOrCreateAsync("b", _ => Task.FromResult(2), CancellationToken.None);

        cache.InvalidateAll();

        var aCalls = 0;
        var bCalls = 0;
        var a = await cache.GetOrCreateAsync("a", _ => { aCalls++; return Task.FromResult(10); }, CancellationToken.None);
        var b = await cache.GetOrCreateAsync("b", _ => { bCalls++; return Task.FromResult(20); }, CancellationToken.None);

        Assert.Equal(10, a);
        Assert.Equal(20, b);
        Assert.Equal(1, aCalls);
        Assert.Equal(1, bCalls);
    }
}
