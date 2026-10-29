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
            .AddScoped<IUserDirectory, IdentityUserDirectory>();

    /// <summary>Applies any migrations the database has not had yet. Development only; see Program.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<QualiTrackDbContext>();
        await context.Database.MigrateAsync();
    }
}
