using Microsoft.EntityFrameworkCore;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Infrastructure.Persistence;

public sealed class QualiTrackDbContext(DbContextOptions<QualiTrackDbContext> options) : DbContext(options)
{
    public DbSet<NonConformance> NonConformances => Set<NonConformance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QualiTrackDbContext).Assembly);
}
