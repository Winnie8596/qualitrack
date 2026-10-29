using QualiTrack.Application.Abstractions;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Tests.Fakes;

internal sealed class InMemoryNcrRepository : INcrRepository
{
    private readonly Dictionary<Guid, NonConformance> _ncrs = [];

    public IReadOnlyCollection<NonConformance> All => _ncrs.Values;

    public Task<NonConformance?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_ncrs.GetValueOrDefault(id));

    public void Add(NonConformance ncr) => _ncrs.Add(ncr.Id, ncr);
}
