using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QualiTrack.Application.Abstractions;
using QualiTrack.Application.Dashboard;
using QualiTrack.Domain.NonConformances;
using QualiTrack.Infrastructure.Caching;
using StackExchange.Redis;

namespace QualiTrack.Api.IntegrationTests.Caching;

public sealed class CachedDashboardReaderTests
{
    private static readonly DashboardPeriod _period = new(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-90));

    private readonly CountingReader _database = new();
    private readonly IDistributedCache _memory = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    [Fact]
    public async Task A_second_read_within_the_time_to_live_comes_from_the_cache_not_the_database()
    {
        var reader = Reader(_memory);

        var first = await reader.ReadAsync(_period, CancellationToken.None);
        var second = await reader.ReadAsync(_period, CancellationToken.None);

        Assert.Equal(1, _database.Reads);
        Assert.Equal(first.OverdueCount, second.OverdueCount);
        Assert.Equal(first.UnfinishedBySeverity, second.UnfinishedBySeverity);
        Assert.Equal(first.AverageDaysToClose, second.AverageDaysToClose);
    }

    [Fact]
    public async Task After_the_cache_is_invalidated_the_next_read_goes_to_the_database()
    {
        var cache = new DashboardCache(_memory, NullLogger<DashboardCache>.Instance);
        var reader = new CachedDashboardReader(_database, cache);
        await reader.ReadAsync(_period, CancellationToken.None);

        await cache.InvalidateAsync(CancellationToken.None);
        await reader.ReadAsync(_period, CancellationToken.None);

        Assert.Equal(2, _database.Reads);
    }

    [Fact]
    public async Task The_numbers_are_cached_for_sixty_seconds()
    {
        var cache = new RecordingCache(_memory);

        await Reader(cache).ReadAsync(_period, CancellationToken.None);

        Assert.Equal(TimeSpan.FromSeconds(60), cache.LastOptions?.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task When_the_cache_is_down_every_read_still_gets_the_numbers_from_the_database()
    {
        var reader = Reader(new UnreachableCache());

        var view = await reader.ReadAsync(_period, CancellationToken.None);
        await reader.ReadAsync(_period, CancellationToken.None);

        Assert.Equal(CountingReader.View.OverdueCount, view.OverdueCount);
        Assert.Equal(2, _database.Reads);
    }

    [Fact]
    public async Task Invalidating_a_cache_that_is_down_does_not_fail_the_change_that_asked_for_it()
    {
        var cache = new DashboardCache(new UnreachableCache(), NullLogger<DashboardCache>.Instance);

        var exception = await Record.ExceptionAsync(() => cache.InvalidateAsync(CancellationToken.None));

        Assert.Null(exception);
    }

    private CachedDashboardReader Reader(IDistributedCache cache) =>
        new(_database, new DashboardCache(cache, NullLogger<DashboardCache>.Instance));

    private sealed class CountingReader : IDashboardReader
    {
        public static readonly DashboardView View = new([new SeverityCount(Severity.Major, 2)], OverdueCount: 1, AverageDaysToClose: 4.5);

        public int Reads { get; private set; }

        public Task<DashboardView> ReadAsync(DashboardPeriod period, CancellationToken cancellationToken)
        {
            Reads++;
            return Task.FromResult(View);
        }
    }

    private sealed class RecordingCache(IDistributedCache inner) : IDistributedCache
    {
        public DistributedCacheEntryOptions? LastOptions { get; private set; }

        public byte[]? Get(string key) => inner.Get(key);

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => inner.GetAsync(key, token);

        public void Refresh(string key) => inner.Refresh(key);

        public Task RefreshAsync(string key, CancellationToken token = default) => inner.RefreshAsync(key, token);

        public void Remove(string key) => inner.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default) => inner.RemoveAsync(key, token);

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            LastOptions = options;
            inner.Set(key, value, options);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            LastOptions = options;
            return inner.SetAsync(key, value, options, token);
        }
    }

    // What the Redis cache throws when it cannot connect
    private sealed class UnreachableCache : IDistributedCache
    {
        public byte[]? Get(string key) => throw Down();

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => throw Down();

        public void Refresh(string key) => throw Down();

        public Task RefreshAsync(string key, CancellationToken token = default) => throw Down();

        public void Remove(string key) => throw Down();

        public Task RemoveAsync(string key, CancellationToken token = default) => throw Down();

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => throw Down();

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
            throw Down();

        private static RedisConnectionException Down() =>
            new(ConnectionFailureType.UnableToConnect, "It was not possible to connect to the redis server(s).");
    }
}
