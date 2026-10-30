using Microsoft.EntityFrameworkCore;
using QualiTrack.Application.Abstractions;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Infrastructure.Persistence;

public sealed class NcrRepository(QualiTrackDbContext context) : INcrRepository
{
    // Actions and history are JSON columns on the NCR row, so this one query loads the whole aggregate
    public Task<NonConformance?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.NonConformances.SingleOrDefaultAsync(ncr => ncr.Id == id, cancellationToken);

    public void Add(NonConformance ncr) => context.NonConformances.Add(ncr);
}
