using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Infrastructure.Persistence;

// Users live in the same database as NCRs, keyed by Guid, so an Identity user's id is the domain's UserId
public sealed class QualiTrackDbContext(DbContextOptions<QualiTrackDbContext> options)
    : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<NonConformance> NonConformances => Set<NonConformance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QualiTrackDbContext).Assembly);
    }
}
