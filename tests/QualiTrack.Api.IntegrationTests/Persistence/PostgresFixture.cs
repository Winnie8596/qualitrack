using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using QualiTrack.Domain.NonConformances;
using QualiTrack.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace QualiTrack.Api.IntegrationTests.Persistence;

/// <summary>
/// A real PostgreSQL in Docker (same major version as docker-compose), started once and shared by every test
/// class in the <see cref="PostgresCollection"/>. Tests share the database, so each uses its own NCR number.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private int _lastSequence;

    public QualiTrackDbContext CreateContext(params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<QualiTrackDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .AddInterceptors(interceptors)
            .Options);

    public NcrNumber NextNumber() => NcrNumber.From(2026, Interlocked.Increment(ref _lastSequence));

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
