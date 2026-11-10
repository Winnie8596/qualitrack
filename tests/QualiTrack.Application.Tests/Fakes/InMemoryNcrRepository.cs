using QualiTrack.Application.Abstractions;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Tests.Fakes;

internal sealed class InMemoryNcrRepository : INcrRepository
{
    private readonly Dictionary<Guid, NonConformance> _ncrs = [];

    public IReadOnlyCollection<NonConformance> All => _ncrs.Values;

    public Task<NonConformance?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_ncrs.GetValueOrDefault(id));

    // Applies the filters for real; sort order is covered against PostgreSQL in the repository tests
    public Task<IReadOnlyList<NonConformance>> ListAsync(NcrListCriteria criteria, CancellationToken cancellationToken)
    {
        var matching = criteria.Filters
            .Aggregate(_ncrs.Values.AsQueryable(), (ncrs, filter) => ncrs.Where(filter))
            .Skip(criteria.Skip)
            .Take(criteria.Take);
        return Task.FromResult<IReadOnlyList<NonConformance>>([.. matching]);
    }

    public void Add(NonConformance ncr) => _ncrs.Add(ncr.Id, ncr);
}
