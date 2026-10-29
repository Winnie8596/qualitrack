using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QualiTrack.Application.Abstractions;
using QualiTrack.Infrastructure.Identity;
using QualiTrack.Infrastructure.Persistence;

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

    /// <summary>Applies any migrations the database has not had yet. Development only; see Program.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<QualiTrackDbContext>();
        await context.Database.MigrateAsync();
    }
}
