using Microsoft.EntityFrameworkCore;
using QualiTrack.Application.Abstractions;
using QualiTrack.Application.Dashboard;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Infrastructure.Persistence;

/// <summary>The dashboard's numbers, counted by PostgreSQL; nothing is loaded but the counts.</summary>
public sealed class DatabaseDashboardReader(QualiTrackDbContext context) : IDashboardReader
{
    public async Task<DashboardView> ReadAsync(DashboardPeriod period, CancellationToken cancellationToken)
    {
        var ncrs = context.NonConformances.AsNoTracking();

        var unfinished = await ncrs
            .Where(NonConformance.Unfinished)
            .GroupBy(ncr => ncr.Details.Severity)
            .Select(group => new { Severity = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Severity, group => group.Count, cancellationToken);
        var overdue = await ncrs.CountAsync(NonConformance.OverdueAt(period.Now), cancellationToken);

        return new DashboardView(
            [.. Enum.GetValues<Severity>().Select(severity => new SeverityCount(severity, unfinished.GetValueOrDefault(severity)))],
            overdue,
            await AverageDaysToCloseAsync(period.ClosedSince, cancellationToken));
    }

    // When an NCR was closed is only in its history, a JSON array (see NonConformanceConfiguration), which LINQ
    // cannot reach through the private field EF maps; so this one figure is SQL. The keys are the C# property
    // names EF writes, and the history is append-only (BR-7), so an NCR has at most one change to Closed.
    private async Task<double?> AverageDaysToCloseAsync(DateTimeOffset closedSince, CancellationToken cancellationToken) =>
        await context.Database
            .SqlQuery<double?>(
                $"""
                SELECT AVG(EXTRACT(EPOCH FROM (closed.at - n."Raised_At")) / 86400)::double precision AS "Value"
                FROM "NonConformances" AS n
                CROSS JOIN LATERAL (
                    SELECT (change -> 'Stamp' ->> 'At')::timestamptz AS at
                    FROM jsonb_array_elements(n."History") AS change
                    WHERE change ->> 'To' = {nameof(NcrStatus.Closed)}
                ) AS closed
                WHERE closed.at >= {closedSince}
                """)
            .SingleAsync(cancellationToken);
}
