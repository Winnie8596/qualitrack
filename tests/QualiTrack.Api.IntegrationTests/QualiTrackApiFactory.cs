using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using QualiTrack.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace QualiTrack.Api.IntegrationTests;

/// <summary>
/// The real API in Development, against its own empty PostgreSQL in Docker, shared by every test class in the
/// <see cref="ApiCollection"/>. Separate from the persistence tests' database so startup sees a fresh one.
/// </summary>
public sealed class QualiTrackApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string DemoPassword = "Demo-Password-1";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly string _photos = Path.Combine(Path.GetTempPath(), $"qualitrack-photos-{Guid.NewGuid():N}");

    public QualiTrackDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<QualiTrackDbContext>().UseNpgsql(_database.GetConnectionString()).Options);

    public Task InitializeAsync() => _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
        if (Directory.Exists(_photos))
        {
            Directory.Delete(_photos, recursive: true);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder
            .UseEnvironment("Development")
            .UseSetting("ConnectionStrings:QualiTrack", _database.GetConnectionString())
            .UseSetting("DemoUsers:Inspector", DemoPassword)
            .UseSetting("DemoUsers:Investigator", DemoPassword)
            .UseSetting("DemoUsers:QaManager", DemoPassword)
            .UseSetting("FileStore:Root", _photos)
            // Every test client shares one address, so the real limit would soon refuse other tests' logins;
            // AuthEndpointsTests checks the limit on a host of its own
            .UseSetting("LoginRateLimit:PermitLimit", "1000");
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<QualiTrackApiFactory>
{
    public const string Name = "Api";
}
