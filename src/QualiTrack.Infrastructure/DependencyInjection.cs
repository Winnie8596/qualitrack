using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QualiTrack.Application.Abstractions;
using QualiTrack.Infrastructure.Caching;
using QualiTrack.Infrastructure.Identity;
using QualiTrack.Infrastructure.Persistence;
using StackExchange.Redis;

namespace QualiTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString) =>
        services
            .AddDbContext<QualiTrackDbContext>(options => options.UseNpgsql(connectionString))
            .AddScoped<INcrRepository, NcrRepository>()
            .AddScoped<IUnitOfWork, UnitOfWork>()
            .AddScoped<INcrNumberGenerator, NcrNumberGenerator>()
            .AddScoped<IUserDirectory, IdentityUserDirectory>()
            .AddSingleton<IClock, SystemClock>();

    /// <summary>
    /// The dashboard reader, cache-aside over Redis. Without a Redis connection string (tests, a quick local run)
    /// the cache is this process's memory, which is right for one instance and wrong for several.
    /// </summary>
    public static IServiceCollection AddDashboard(this IServiceCollection services, string? redisConnectionString)
    {
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                var redis = ConfigurationOptions.Parse(redisConnectionString);

                // Start even when Redis is down, and give up on it quickly: the database answers instead
                redis.AbortOnConnectFail = false;
                redis.ConnectTimeout = 1000;
                redis.AsyncTimeout = 1000;
                redis.SyncTimeout = 1000;
                options.ConfigurationOptions = redis;
            });
        }

        return services
            .AddSingleton<DashboardCache>()
            .AddScoped<DatabaseDashboardReader>()
            .AddScoped<IDashboardReader>(provider => new CachedDashboardReader(
                provider.GetRequiredService<DatabaseDashboardReader>(), provider.GetRequiredService<DashboardCache>()));
    }

    /// <summary>Applies any migrations the database has not had yet. Development only; see Program.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<QualiTrackDbContext>();
        await context.Database.MigrateAsync();
    }
}
