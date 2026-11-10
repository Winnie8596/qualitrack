using Microsoft.EntityFrameworkCore;
using QualiTrack.Application.Abstractions;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Infrastructure.Persistence;

public sealed class NcrRepository(QualiTrackDbContext context) : INcrRepository
{
    // Actions and history are JSON columns on the NCR row, so this one query loads the whole aggregate
    public Task<NonConformance?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.NonConformances.SingleOrDefaultAsync(ncr => ncr.Id == id, cancellationToken);

    // Read-only, so EF does not track the entities; one query, since the aggregate is one row
    public async Task<IReadOnlyList<NonConformance>> ListAsync(NcrListCriteria criteria, CancellationToken cancellationToken)
    {
        var matching = criteria.Filters.Aggregate(context.NonConformances.AsNoTracking(), (ncrs, filter) => ncrs.Where(filter));

        // The NCR number breaks ties, so paging never shows a row twice or skips one
        return await Sorted(matching, criteria)
            .ThenBy(ncr => ncr.Number)
            .Skip(criteria.Skip)
            .Take(criteria.Take)
            .ToListAsync(cancellationToken);
    }

    public void Add(NonConformance ncr) => context.NonConformances.Add(ncr);

    private static IOrderedQueryable<NonConformance> Sorted(IQueryable<NonConformance> ncrs, NcrListCriteria criteria) =>
        (criteria.SortBy, criteria.Direction) switch
        {
            (NcrSortField.RaisedAt, SortDirection.Ascending) => ncrs.OrderBy(ncr => ncr.Raised.At),
            (NcrSortField.RaisedAt, _) => ncrs.OrderByDescending(ncr => ncr.Raised.At),
            (NcrSortField.DueDate, SortDirection.Ascending) => NotYetDueLast(ncrs).ThenBy(ncr => ncr.DueDate),
            (NcrSortField.DueDate, _) => NotYetDueLast(ncrs).ThenByDescending(ncr => ncr.DueDate),
            _ => throw new ArgumentOutOfRangeException(nameof(criteria), criteria.SortBy, "Unknown sort field."),
        };

    // PostgreSQL puts NULLs last ascending but first descending; an NCR with no due date belongs last either way
    private static IOrderedQueryable<NonConformance> NotYetDueLast(IQueryable<NonConformance> ncrs) =>
        ncrs.OrderBy(ncr => ncr.DueDate == null);
}
