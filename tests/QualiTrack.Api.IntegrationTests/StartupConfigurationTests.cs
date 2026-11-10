using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QualiTrack.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace QualiTrack.Api.IntegrationTests;

/// <summary>
/// What the app does to its database as it starts is decided by configuration, not by the environment's name, so
/// a deployment can opt in. Each test starts the app in Production against an empty PostgreSQL of its own.
/// </summary>
public sealed class StartupConfigurationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public Task InitializeAsync() => _database.StartAsync();

    public Task DisposeAsync() => _database.DisposeAsync().AsTask();

    [Fact]
    public async Task Outside_development_the_app_leaves_the_database_as_it_is_unless_configured_otherwise()
    {
        await using var api = ProductionApi();

        _ = api.Server;

        await using var context = CreateDbContext();
        Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task The_app_migrates_and_seeds_the_demo_users_wherever_configuration_asks_for_it()
    {
        await using var api = ProductionApi(
            ("Database:MigrateOnStartup", "true"),
            ("DemoData:Seed", "true"),
            ("DemoUsers:Inspector", QualiTrackApiFactory.DemoPassword),
            ("DemoUsers:Investigator", QualiTrackApiFactory.DemoPassword),
            ("DemoUsers:QaManager", QualiTrackApiFactory.DemoPassword));

        _ = api.Server;

        await using var context = CreateDbContext();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        await using var scope = api.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        Assert.NotNull(await users.FindByEmailAsync("qamanager@qualitrack.demo"));
    }

    private QualiTrackDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<QualiTrackDbContext>().UseNpgsql(_database.GetConnectionString()).Options);

    private WebApplicationFactory<Program> ProductionApi(params (string Key, string Value)[] settings) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production").UseSetting("ConnectionStrings:QualiTrack", _database.GetConnectionString());
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        });
}
