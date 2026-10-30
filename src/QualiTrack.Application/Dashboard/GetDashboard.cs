using QualiTrack.Application.Abstractions;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Dashboard;

/// <summary>
/// The QA manager's numbers (D3): unfinished NCRs by severity, how many are overdue, and how many days NCRs closed
/// in the period took from raised to closed. The average is <c>null</c> when none closed, not zero.
/// </summary>
public sealed record DashboardView(IReadOnlyList<SeverityCount> UnfinishedBySeverity, int OverdueCount, double? AverageDaysToClose);

/// <summary>Every severity is listed, with 0 when none is unfinished, so the page shows the same three rows.</summary>
public sealed record SeverityCount(Severity Severity, int Count);

/// <summary>Overdue is judged at <see cref="Now"/>; the average covers NCRs closed on or after <see cref="ClosedSince"/>.</summary>
public sealed record DashboardPeriod(DateTimeOffset Now, DateTimeOffset ClosedSince);

public sealed class GetDashboardHandler(IDashboardReader reader, IClock clock, ICurrentUser currentUser)
{
    private static readonly TimeSpan _closedWithin = TimeSpan.FromDays(90);

    public async Task<Result<DashboardView>> HandleAsync(CancellationToken cancellationToken)
    {
        if (currentUser.Role != Role.QaManager)
        {
            return Error.Forbidden("Only a QA manager can see the dashboard.");
        }

        var now = clock.UtcNow;
        return await reader.ReadAsync(new DashboardPeriod(now, now - _closedWithin), cancellationToken);
    }
}
