using QualiTrack.Application.Abstractions;
using QualiTrack.Application.Dashboard;

namespace QualiTrack.Infrastructure.Caching;

/// <summary>
/// Cache-aside around the database reader: answer from the cache when it has the numbers, otherwise count them
/// and keep them for <see cref="DashboardCache.TimeToLive"/>. The handler cannot tell the two apart.
/// </summary>
public sealed class CachedDashboardReader(IDashboardReader database, DashboardCache cache) : IDashboardReader
{
    public async Task<DashboardView> ReadAsync(DashboardPeriod period, CancellationToken cancellationToken)
    {
        if (await cache.GetAsync(cancellationToken) is { } cached)
        {
            return cached;
        }

        var view = await database.ReadAsync(period, cancellationToken);
        await cache.SetAsync(view, cancellationToken);
        return view;
    }
}
