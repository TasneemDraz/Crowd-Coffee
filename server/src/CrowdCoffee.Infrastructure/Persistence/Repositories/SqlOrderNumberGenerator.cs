using CrowdCoffee.Application.Ordering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CrowdCoffee.Infrastructure.Persistence.Repositories;

/// <summary>
/// T9.2.1: avoids MAX()+1 under concurrency. The UPDATE ... OUTPUT below takes SQL Server's
/// normal row-level X lock for the duration of the caller's ambient transaction (this must be
/// called from inside one — see IOrderNumberGenerator), so two concurrent order creations on the
/// same day serialize on that single counter row instead of racing to read-then-write the same
/// value. First order of a new day: the UPDATE affects zero rows, so this falls through to an
/// INSERT; if a second request loses that exact race (both saw zero rows), its INSERT hits the
/// PK and is retried as an UPDATE, which by then succeeds against the row the winner just created.
/// </summary>
public class SqlOrderNumberGenerator(CrowdCoffeeDbContext dbContext) : IOrderNumberGenerator
{
    public async Task<string> NextAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var dateKey = CairoClock.Today(nowUtc).ToString("yyMMdd");

        var sequence = await TryIncrementAsync(dateKey, cancellationToken);
        if (sequence is null)
        {
            try
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO dbo.OrderNumberCounters (DateKey, LastValue) VALUES ({dateKey}, 1)", cancellationToken);
                sequence = 1;
            }
            catch (SqlException ex) when (ex.Number is 2627 or 2601)
            {
                sequence = await TryIncrementAsync(dateKey, cancellationToken)
                    ?? throw new InvalidOperationException($"Order number counter for '{dateKey}' vanished between INSERT conflict and retry.");
            }
        }

        return $"CRW-{dateKey}-{sequence:D4}";
    }

    private async Task<int?> TryIncrementAsync(string dateKey, CancellationToken cancellationToken)
    {
        var results = await dbContext.Database
            .SqlQuery<int>($"UPDATE dbo.OrderNumberCounters SET LastValue = LastValue + 1 OUTPUT INSERTED.LastValue WHERE DateKey = {dateKey}")
            .ToListAsync(cancellationToken);

        return results.Count > 0 ? results[0] : null;
    }
}
