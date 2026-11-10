using QualiTrack.Application.Dashboard;

namespace QualiTrack.Application.Abstractions;

/// <summary>
/// Counts over every NCR, which the database does far better than loading each one. The implementation may serve
/// a recent answer from a cache: the dashboard is allowed to be up to a minute old (PRD §8, plan.md S6).
/// </summary>
public interface IDashboardReader
{
    Task<DashboardView> ReadAsync(DashboardPeriod period, CancellationToken cancellationToken);
}
