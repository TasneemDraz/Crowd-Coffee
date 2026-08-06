namespace CrowdCoffee.Application.Dashboard;

public record TopSellingItemDto(string ProductName, int QuantitySold);

/// Repository-facing shape for "today" — IDashboardRepository computes the shop's Cairo-local day
/// itself (see IDashboardRepository's doc comment), so Date here is that resolved day, not
/// whatever UTC date the caller happened to pass in.
public record DashboardDayAggregate(
    DateOnly Date,
    decimal Revenue,
    int OrderCount,
    int DeliveryCount,
    int PickupCount,
    IReadOnlyList<TopSellingItemDto> TopSellingItems);

public record DailyDashboardDto(
    DateOnly Date,
    decimal Revenue,
    int OrderCount,
    decimal AverageOrderValue,
    int DeliveryCount,
    int PickupCount,
    IReadOnlyList<TopSellingItemDto> TopSellingItems,
    int LowStockCount);
