using Microsoft.EntityFrameworkCore;
using QualiTrack.Application.Abstractions;
using QualiTrack.Domain.NonConformances;
using QualiTrack.Infrastructure.Caching;

namespace QualiTrack.Infrastructure.Persistence;

/// <summary>EF Core already tracks every change a handler makes; saving writes them all in one transaction.</summary>
public sealed class UnitOfWork(QualiTrackDbContext context, DashboardCache dashboardCache) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        var changesDashboard = ChangesDashboardNumbers();

        await context.SaveChangesAsync(cancellationToken);

        // Here rather than in each handler, so no command that changes the numbers can forget to
        if (changesDashboard)
        {
            await dashboardCache.InvalidateAsync(cancellationToken);
        }
    }

    // The dashboard counts NCRs by status (and due date, which only changes with it); adding an action does not
    private bool ChangesDashboardNumbers()
    {
        context.ChangeTracker.DetectChanges();
        return context.ChangeTracker.Entries<NonConformance>()
            .Any(entry => entry.State == EntityState.Added || entry.Property(ncr => ncr.Status).IsModified);
    }
}
