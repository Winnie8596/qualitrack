using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QualiTrack.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> build the context without starting the API. Creating a migration only
/// compares the model with the last snapshot, so it never connects and needs no real credentials.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<QualiTrackDbContext>
{
    public QualiTrackDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<QualiTrackDbContext>().UseNpgsql("Host=localhost;Database=qualitrack").Options);
}
