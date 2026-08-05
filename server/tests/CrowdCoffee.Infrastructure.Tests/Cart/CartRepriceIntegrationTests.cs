using CrowdCoffee.Application.Cart;
using CrowdCoffee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CrowdCoffee.Infrastructure.Tests.Cart;

/// <summary>
/// Exercises RepriceCartHandler against the real seeded menu data — the point is proving the
/// EF-backed ICatalogRepository lookups (product by id, modifier groups by category) and the
/// pure CartLineRepricer logic actually compose correctly together, not just in isolation.
/// </summary>
public class CartRepriceIntegrationTests
{
    [Fact]
    public async Task Reprice_RecomputesUnitPriceFromLiveDbPricesAndDeltas()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<RepriceCartHandler>();

        var product = await dbContext.Products.AsNoTracking().FirstAsync(p => p.CategorySlug == "classic" && p.IsActive);
        var milkGroup = await dbContext.ModifierGroups.AsNoTracking()
            .Include(g => g.Options)
            .FirstAsync(g => g.Slug == "milk");
        var oatMilk = milkGroup.Options.First(o => o.Slug == "oat");
        var shotGroup = await dbContext.ModifierGroups.AsNoTracking().Include(g => g.Options).FirstAsync(g => g.Slug == "shot");
        var shot = shotGroup.Options.First();

        var request = new CartRepriceRequest([new CartLineRepriceRequest(product.Id, oatMilk.Id, shot.Id, [], 2)]);

        var result = await handler.HandleAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var line = Assert.Single(result.Value.Lines);
        Assert.Equal(product.Price + oatMilk.PriceDelta + shot.PriceDelta, line.UnitPrice);
        Assert.Equal(line.UnitPrice * 2, line.LineTotal);
        Assert.Equal(line.LineTotal, result.Value.Subtotal);
        Assert.StartsWith($"{product.Id}::{oatMilk.Id}::shot::", line.Id);
        Assert.Contains(oatMilk.Name, line.Name);
        Assert.Contains(shot.Name, line.Name);
    }

    [Fact]
    public async Task Reprice_MultipleLines_SumsIntoSubtotal()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<RepriceCartHandler>();

        var products = await dbContext.Products.AsNoTracking().Where(p => p.CategorySlug == "black" && p.IsActive).Take(2).ToListAsync();
        var milkGroup = await dbContext.ModifierGroups.AsNoTracking().Include(g => g.Options).FirstAsync(g => g.Slug == "milk");
        var noMilk = milkGroup.Options.First(o => o.Slug == "none");

        var request = new CartRepriceRequest(products
            .Select(p => new CartLineRepriceRequest(p.Id, noMilk.Id, null, [], 1))
            .ToList());

        var result = await handler.HandleAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Lines.Count);
        Assert.Equal(result.Value.Lines.Sum(l => l.LineTotal), result.Value.Subtotal);
    }

    [Fact]
    public async Task Reprice_MilkOptionFromWrongProduct_RejectsWholeRequest()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrowdCoffeeDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<RepriceCartHandler>();

        var product = await dbContext.Products.AsNoTracking().FirstAsync(p => p.CategorySlug == "classic" && p.IsActive);
        var syrupGroup = await dbContext.ModifierGroups.AsNoTracking().Include(g => g.Options).FirstAsync(g => g.Slug == "syrups");
        // A syrup option id submitted as the milk option id — genuinely foreign to the milk group.
        var foreignId = syrupGroup.Options.First().Id;

        var request = new CartRepriceRequest([new CartLineRepriceRequest(product.Id, foreignId, null, [], 1)]);

        var result = await handler.HandleAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Cart.InvalidMilkOption", result.Error!.Code);
    }

    [Fact]
    public async Task Reprice_UnknownProductId_ReturnsNotFound()
    {
        await using var provider = TestServiceProviderFactory.Build();
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<RepriceCartHandler>();

        var request = new CartRepriceRequest([new CartLineRepriceRequest(Guid.NewGuid(), Guid.NewGuid(), null, [], 1)]);

        var result = await handler.HandleAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Cart.ProductNotFound", result.Error!.Code);
    }
}
