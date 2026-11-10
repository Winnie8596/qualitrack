using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using QualiTrack.Api.DemoData;
using QualiTrack.Domain.NonConformances;
using QualiTrack.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace QualiTrack.Api.IntegrationTests.DemoData;

/// <summary>The app, started with demo data on against an empty PostgreSQL of its own, so the counts are exact.</summary>
public sealed class DemoNcrSeedTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public Task InitializeAsync() => _database.StartAsync();

    public Task DisposeAsync() => _database.DisposeAsync().AsTask();

    [Fact]
    public async Task The_demo_starts_with_fifty_ncrs_in_every_status_some_of_them_overdue()
    {
        await using var api = DemoApi();

        _ = api.Server;

        var ncrs = await SeededNcrsAsync();
        var now = DateTimeOffset.UtcNow;
        Assert.Equal(50, ncrs.Count);
        Assert.Equal(Enum.GetValues<NcrStatus>().Order(), ncrs.Select(ncr => ncr.Status).Distinct().Order());
        Assert.Equal(Enum.GetValues<Severity>().Order(), ncrs.Select(ncr => ncr.Details.Severity).Distinct().Order());
        Assert.Contains(ncrs, ncr => ncr.IsOverdue(now));
        Assert.Contains(ncrs, ncr => !ncr.IsFinished && ncr.DueDate > now);
    }

    [Fact]
    public async Task Nothing_in_the_demo_is_stamped_in_the_future()
    {
        await using var api = DemoApi();

        _ = api.Server;

        var now = DateTimeOffset.UtcNow;
        Assert.All(await SeededNcrsAsync(), ncr =>
        {
            Assert.True(ncr.Raised.At <= now);
            Assert.All(ncr.History, change => Assert.True(change.Stamp.At <= now));
            Assert.All(ncr.Actions, action => Assert.True((action.Completed?.At ?? action.Added.At) <= now));
        });
    }

    [Fact]
    public async Task Seeding_again_adds_nothing_to_a_database_that_already_has_ncrs()
    {
        await using var api = DemoApi();
        _ = api.Server;

        await api.Services.SeedDemoNcrsAsync();

        Assert.Equal(50, (await SeededNcrsAsync()).Count);
    }

    private async Task<List<NonConformance>> SeededNcrsAsync()
    {
        await using var context = new QualiTrackDbContext(
            new DbContextOptionsBuilder<QualiTrackDbContext>().UseNpgsql(_database.GetConnectionString()).Options);
        return await context.NonConformances.AsNoTracking().ToListAsync();
    }

    private WebApplicationFactory<Program> DemoApi() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .UseSetting("ConnectionStrings:QualiTrack", _database.GetConnectionString())
            .UseSetting("Database:MigrateOnStartup", "true")
            .UseSetting("DemoData:Seed", "true")
            .UseSetting("DemoUsers:Inspector", QualiTrackApiFactory.DemoPassword)
            .UseSetting("DemoUsers:Investigator", QualiTrackApiFactory.DemoPassword)
            .UseSetting("DemoUsers:QaManager", QualiTrackApiFactory.DemoPassword));
}
