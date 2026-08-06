namespace CrowdCoffee.Application.Dashboard;

/// <summary>
/// Mirrors SqlOrderNumberGenerator's (Epic 9.2) reasoning for keeping Cairo-local-day resolution
/// an Infrastructure concern: Application can't reference CairoClock (Infrastructure) directly
/// under this project's one-way layering, so the repository takes the raw UTC instant and
/// resolves "today" itself, rather than the caller pre-computing a day range it can't correctly
/// derive without the same timezone logic.
/// </summary>
public interface IDashboardRepository
{
    Task<DashboardDayAggregate> GetTodayAggregateAsync(DateTime nowUtc, CancellationToken cancellationToken);
}
