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
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public QualiTrackDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<QualiTrackDbContext>().UseNpgsql(_database.GetConnectionString()).Options);

    public Task InitializeAsync() => _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder
            .UseEnvironment("Development")
            .UseSetting("ConnectionStrings:QualiTrack", _database.GetConnectionString());
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<QualiTrackApiFactory>
{
    public const string Name = "Api";
}
