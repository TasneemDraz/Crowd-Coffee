using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Domain.Catalog;

namespace CrowdCoffee.Application.Tests.Catalog;

public class UpdateModifierOptionHandlerTests
{
    [Fact]
    public async Task ExistingOption_UpdatesPriceDeltaAndActiveState()
    {
        var option = new ModifierOption(Guid.NewGuid(), Guid.NewGuid(), "oat", "Oat Milk", 40, 1);
        var repository = new FakeAdminCatalogRepository(modifierOptions: [option]);
        var cache = new FakeCatalogCache();
        var handler = new UpdateModifierOptionHandler(repository, cache);

        var result = await handler.HandleAsync(option.Id, new UpdateModifierOptionRequest(45, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(45, result.Value.PriceDelta);
        Assert.False(result.Value.IsActive);
        Assert.Equal(1, cache.InvalidateCallCount);
    }

    [Fact]
    public async Task UnknownOption_ReturnsNotFound()
    {
        var repository = new FakeAdminCatalogRepository();
        var cache = new FakeCatalogCache();
        var handler = new UpdateModifierOptionHandler(repository, cache);

        var result = await handler.HandleAsync(Guid.NewGuid(), new UpdateModifierOptionRequest(10, true), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("ModifierOption.NotFound", result.Error!.Code);
    }
}
