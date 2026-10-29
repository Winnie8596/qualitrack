using Microsoft.EntityFrameworkCore;
using QualiTrack.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace QualiTrack.Api.IntegrationTests.Persistence;

/// <summary>A real PostgreSQL in Docker, shared by the tests in one class. Same major version as docker-compose.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public QualiTrackDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<QualiTrackDbContext>().UseNpgsql(_container.GetConnectionString()).Options);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
