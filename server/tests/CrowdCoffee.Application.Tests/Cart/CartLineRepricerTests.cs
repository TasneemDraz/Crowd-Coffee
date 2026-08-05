using CrowdCoffee.Application.Cart;
using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Domain.Catalog;
using CrowdCoffee.Domain.Common;

namespace CrowdCoffee.Application.Tests.Cart;

public class CartLineRepricerTests
{
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid OatMilkId = Guid.NewGuid();
    private static readonly Guid FullCreamMilkId = Guid.NewGuid();
    private static readonly Guid ShotId = Guid.NewGuid();
    private static readonly Guid VanillaSyrupId = Guid.NewGuid();
    private static readonly Guid CaramelSyrupId = Guid.NewGuid();

    private static readonly ProductCatalogRow Product =
        new(ProductId, "classic", "c-latte", "Latte", "Espresso-based.", 100, null, 5, true);

    private static readonly IReadOnlyList<ModifierGroupCatalogRow> Groups =
    [
        new ModifierGroupCatalogRow(Guid.NewGuid(), "milk", "Milk Type", ModifierSelectionType.ExactlyOne, 1, true, 0,
        [
            new ModifierOptionCatalogRow(FullCreamMilkId, "full", "Full Cream Milk", 0, 0, true),
            new ModifierOptionCatalogRow(OatMilkId, "oat", "Oat Milk", 40, 1, true),
        ]),
        new ModifierGroupCatalogRow(Guid.NewGuid(), "shot", "Espresso Shot", ModifierSelectionType.UpToN, 1, false, 1,
        [
            new ModifierOptionCatalogRow(ShotId, "espresso", "Espresso Shot", 40, 0, true),
        ]),
        new ModifierGroupCatalogRow(Guid.NewGuid(), "syrups", "Syrups", ModifierSelectionType.UpToN, 2, false, 2,
        [
            new ModifierOptionCatalogRow(VanillaSyrupId, "vanilla", "Vanilla Syrup", 30, 0, true),
            new ModifierOptionCatalogRow(CaramelSyrupId, "caramel", "Caramel Syrup", 30, 1, true),
        ]),
    ];

    private static CartLineRepriceRequest Request(
        Guid milkId, Guid? shotId = null, List<Guid>? syrupIds = null, int qty = 1) =>
        new(ProductId, milkId, shotId, syrupIds ?? [], qty);

    [Fact]
    public void MilkOnly_ComputesUnitPriceFromBasePlusMilkDelta()
    {
        var result = CartLineRepricer.Reprice(Product, Groups, Request(OatMilkId));

        Assert.True(result.IsSuccess);
        Assert.Equal(140, result.Value.UnitPrice); // 100 base + 40 oat
        Assert.Equal("Latte (Oat Milk)", result.Value.Name);
    }

    [Fact]
    public void ShotAndSyrups_AddToUnitPrice_AndComposeDisplayName()
    {
        var result = CartLineRepricer.Reprice(
            Product, Groups, Request(OatMilkId, ShotId, [VanillaSyrupId, CaramelSyrupId], qty: 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(240, result.Value.UnitPrice); // 100 + 40 oat + 40 shot + 30 vanilla + 30 caramel
        Assert.Equal(480, result.Value.LineTotal); // ×2
        Assert.Equal("Latte (Oat Milk, + Espresso Shot, + Vanilla Syrup, + Caramel Syrup)", result.Value.Name);
    }

    [Fact]
    public void ComposedId_MatchesFrontendScheme_SortedSyrupsAndShotMarker()
    {
        var result = CartLineRepricer.Reprice(
            Product, Groups, Request(OatMilkId, ShotId, [CaramelSyrupId, VanillaSyrupId]));

        Assert.True(result.IsSuccess);
        var expectedSyrupPart = string.Join("-", new[] { VanillaSyrupId, CaramelSyrupId }.Select(id => id.ToString()).OrderBy(s => s, StringComparer.Ordinal));
        Assert.Equal($"{ProductId}::{OatMilkId}::shot::{expectedSyrupPart}", result.Value.Id);
    }

    [Fact]
    public void ComposedId_CollapsesRegardlessOfSyrupSubmissionOrder()
    {
        var a = CartLineRepricer.Reprice(Product, Groups, Request(OatMilkId, syrupIds: [VanillaSyrupId, CaramelSyrupId]));
        var b = CartLineRepricer.Reprice(Product, Groups, Request(OatMilkId, syrupIds: [CaramelSyrupId, VanillaSyrupId]));

        Assert.Equal(a.Value.Id, b.Value.Id);
    }

    [Fact]
    public void NoShotSelected_IdUsesNoshotMarker()
    {
        var result = CartLineRepricer.Reprice(Product, Groups, Request(OatMilkId));

        Assert.Contains("::noshot::", result.Value.Id);
    }

    [Fact]
    public void MilkOptionForeignToProduct_IsRejected()
    {
        var result = CartLineRepricer.Reprice(Product, Groups, Request(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Equal("Cart.InvalidMilkOption", result.Error.Code);
    }

    [Fact]
    public void ShotOptionForeignToProduct_IsRejected()
    {
        var result = CartLineRepricer.Reprice(Product, Groups, Request(OatMilkId, shotId: Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("Cart.InvalidShotOption", result.Error!.Code);
    }

    [Fact]
    public void SyrupOptionForeignToProduct_IsRejected()
    {
        var result = CartLineRepricer.Reprice(Product, Groups, Request(OatMilkId, syrupIds: [Guid.NewGuid()]));

        Assert.True(result.IsFailure);
        Assert.Equal("Cart.InvalidSyrupOption", result.Error!.Code);
    }

    [Fact]
    public void MoreThanTwoSyrups_IsRejected()
    {
        var result = CartLineRepricer.Reprice(
            Product, Groups, Request(OatMilkId, syrupIds: [VanillaSyrupId, CaramelSyrupId, Guid.NewGuid()]));

        Assert.True(result.IsFailure);
        Assert.Equal("Cart.TooManySyrups", result.Error!.Code);
    }

    [Fact]
    public void DuplicateSyrupSelection_IsRejected()
    {
        var result = CartLineRepricer.Reprice(Product, Groups, Request(OatMilkId, syrupIds: [VanillaSyrupId, VanillaSyrupId]));

        Assert.True(result.IsFailure);
        Assert.Equal("Cart.DuplicateSyrups", result.Error!.Code);
    }

    [Fact]
    public void ZeroOrNegativeQuantity_IsRejected()
    {
        var result = CartLineRepricer.Reprice(Product, Groups, Request(OatMilkId, qty: 0));

        Assert.True(result.IsFailure);
        Assert.Equal("Cart.InvalidQuantity", result.Error!.Code);
    }
}
