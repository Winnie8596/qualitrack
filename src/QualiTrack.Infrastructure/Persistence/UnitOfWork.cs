using QualiTrack.Application.Abstractions;

namespace QualiTrack.Infrastructure.Persistence;

/// <summary>EF Core already tracks every change a handler makes; saving writes them all in one transaction.</summary>
public sealed class UnitOfWork(QualiTrackDbContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
