using CrowdCoffee.Domain.Inventory;

namespace CrowdCoffee.Domain.Tests.Inventory;

public class StockItemTests
{
    private static StockItem CreateStockItem(decimal onHand = 100, decimal reserved = 0, decimal reorderPoint = 20)
    {
        var item = new StockItem(Guid.NewGuid(), "Coffee Beans", StockUnit.Gram, reorderPoint, onHand);
        if (reserved > 0)
        {
            item.Reserve(reserved);
        }

        return item;
    }

    // --- Reserve ---

    [Fact]
    public void Reserve_WithSufficientAvailable_Succeeds()
    {
        var item = CreateStockItem(onHand: 100);

        var result = item.Reserve(40);

        Assert.True(result.IsSuccess);
        Assert.Equal(40, item.QuantityReserved);
        Assert.Equal(60, item.Available);
    }

    [Fact]
    public void Reserve_ExactlyAvailable_Succeeds()
    {
        var item = CreateStockItem(onHand: 100);

        var result = item.Reserve(100);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, item.Available);
    }

    [Fact]
    public void Reserve_MoreThanAvailable_Fails()
    {
        var item = CreateStockItem(onHand: 100);

        var result = item.Reserve(101);

        Assert.True(result.IsFailure);
        Assert.Equal(0, item.QuantityReserved);
    }

    [Fact]
    public void Reserve_ZeroStock_Fails()
    {
        var item = CreateStockItem(onHand: 0);

        var result = item.Reserve(1);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Reserve_NonPositiveQuantity_Fails()
    {
        var item = CreateStockItem(onHand: 100);

        Assert.True(item.Reserve(0).IsFailure);
        Assert.True(item.Reserve(-5).IsFailure);
    }

    [Fact]
    public void Reserve_AccountsForExistingReservations()
    {
        var item = CreateStockItem(onHand: 100, reserved: 60);

        var result = item.Reserve(41);

        Assert.True(result.IsFailure);
    }

    // --- ReleaseReservation ---

    [Fact]
    public void ReleaseReservation_ReducesReservedQuantity()
    {
        var item = CreateStockItem(onHand: 100, reserved: 40);

        item.ReleaseReservation(15);

        Assert.Equal(25, item.QuantityReserved);
    }

    [Fact]
    public void ReleaseReservation_ClampsAtZero_DoesNotThrow()
    {
        var item = CreateStockItem(onHand: 100, reserved: 10);

        item.ReleaseReservation(999);

        Assert.Equal(0, item.QuantityReserved);
    }

    [Fact]
    public void ReleaseReservation_NonPositiveQuantity_IsNoOp()
    {
        var item = CreateStockItem(onHand: 100, reserved: 40);

        item.ReleaseReservation(0);
        item.ReleaseReservation(-5);

        Assert.Equal(40, item.QuantityReserved);
    }

    // --- Fulfill ---

    [Fact]
    public void Fulfill_WithSufficientReservation_DecrementsBothReservedAndOnHand()
    {
        var item = CreateStockItem(onHand: 100, reserved: 40);

        var result = item.Fulfill(40);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, item.QuantityReserved);
        Assert.Equal(60, item.QuantityOnHand);
    }

    [Fact]
    public void Fulfill_MoreThanReserved_Fails()
    {
        var item = CreateStockItem(onHand: 100, reserved: 20);

        var result = item.Fulfill(21);

        Assert.True(result.IsFailure);
        Assert.Equal(20, item.QuantityReserved);
        Assert.Equal(100, item.QuantityOnHand);
    }

    [Fact]
    public void Fulfill_NonPositiveQuantity_Fails()
    {
        var item = CreateStockItem(onHand: 100, reserved: 40);

        Assert.True(item.Fulfill(0).IsFailure);
    }

    // --- AdjustOnHand (Purchase/Waste/Adjustment) ---

    [Fact]
    public void AdjustOnHand_PositiveDelta_IncreasesOnHand()
    {
        var item = CreateStockItem(onHand: 100);

        var result = item.AdjustOnHand(50);

        Assert.True(result.IsSuccess);
        Assert.Equal(150, item.QuantityOnHand);
    }

    [Fact]
    public void AdjustOnHand_NegativeDeltaWithinBounds_DecreasesOnHand()
    {
        var item = CreateStockItem(onHand: 100);

        var result = item.AdjustOnHand(-30);

        Assert.True(result.IsSuccess);
        Assert.Equal(70, item.QuantityOnHand);
    }

    [Fact]
    public void AdjustOnHand_ExactlyToZero_Succeeds()
    {
        var item = CreateStockItem(onHand: 100);

        var result = item.AdjustOnHand(-100);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, item.QuantityOnHand);
    }

    [Fact]
    public void AdjustOnHand_WouldGoNegative_FailsAndLeavesOnHandUnchanged()
    {
        var item = CreateStockItem(onHand: 100);

        var result = item.AdjustOnHand(-101);

        Assert.True(result.IsFailure);
        Assert.Equal(100, item.QuantityOnHand);
    }

    // --- Reconcile ---

    [Fact]
    public void Reconcile_OverwritesOnHand_RegardlessOfPreviousValue()
    {
        var item = CreateStockItem(onHand: 999);

        item.Reconcile(4964);

        Assert.Equal(4964, item.QuantityOnHand);
    }
}
