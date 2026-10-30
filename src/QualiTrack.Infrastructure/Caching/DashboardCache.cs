using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using QualiTrack.Application.Dashboard;
using StackExchange.Redis;

namespace QualiTrack.Infrastructure.Caching;

/// <summary>
/// The dashboard's last numbers in the distributed cache (Redis in docker compose). The cache only ever saves
/// work: when it cannot be reached, a read misses and a write or invalidation is skipped, so callers carry on
/// against the database and nothing fails because Redis is down.
/// </summary>
public sealed partial class DashboardCache(IDistributedCache cache, ILogger<DashboardCache> logger)
{
    public static readonly TimeSpan TimeToLive = TimeSpan.FromSeconds(60);

    // Versioned, so a change to DashboardView's shape never reads an entry written in the old one
    private const string Key = "qualitrack:dashboard:v1";

    public async Task<DashboardView?> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await cache.GetAsync(Key, cancellationToken);
            return bytes is null ? null : JsonSerializer.Deserialize<DashboardView>(bytes);
        }
        catch (Exception exception) when (IsCacheUnavailable(exception))
        {
            LogUnavailable(exception, "read");
            return null;
        }
    }

    public async Task SetAsync(DashboardView view, CancellationToken cancellationToken)
    {
        try
        {
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeToLive };
            await cache.SetAsync(Key, JsonSerializer.SerializeToUtf8Bytes(view), options, cancellationToken);
        }
        catch (Exception exception) when (IsCacheUnavailable(exception))
        {
            LogUnavailable(exception, "write");
        }
    }

    /// <summary>After an NCR is raised or changes status: the next read counts again rather than wait out the TTL.</summary>
    public async Task InvalidateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await cache.RemoveAsync(Key, cancellationToken);
        }
        catch (Exception exception) when (IsCacheUnavailable(exception))
        {
            // Nothing stale can be served while the cache is down, and an entry written before it went down
            // expires within the TTL
            LogUnavailable(exception, "invalidate");
        }
    }

    // Connection failures are RedisException; command timeouts are TimeoutException. Anything else is a bug.
    private static bool IsCacheUnavailable(Exception exception) => exception is RedisException or TimeoutException;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Dashboard cache unavailable for {Operation}; using the database")]
    private partial void LogUnavailable(Exception exception, string operation);
}
