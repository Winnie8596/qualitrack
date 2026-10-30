using System.Linq.Expressions;
using QualiTrack.Application.Abstractions;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.NonConformances;

public sealed record ListNcrsQuery(
    NcrStatus? Status = null,
    Severity? Severity = null,
    bool OverdueOnly = false,
    NcrSortField SortBy = NcrSortField.RaisedAt,
    SortDirection Direction = SortDirection.Descending,
    int Page = 1);

/// <summary>One row of the NCR list (D1).</summary>
public sealed record NcrSummaryView(
    Guid Id,
    string Number,
    string Title,
    Severity Severity,
    NcrStatus Status,
    DateTimeOffset RaisedAt,
    DateTimeOffset? DueDate,
    bool IsOverdue);

/// <summary>
/// A page of the list. It says whether another page follows rather than giving a total, so the list costs one
/// SQL query, not a second one to count every visible NCR.
/// </summary>
public sealed record NcrListPage(IReadOnlyList<NcrSummaryView> Items, int Page, bool HasNextPage);

public sealed class ListNcrsHandler(INcrRepository repository, IClock clock, ICurrentUser currentUser)
{
    public const int PageSize = 20;

    public async Task<Result<NcrListPage>> HandleAsync(ListNcrsQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1)
        {
            return Error.Validation("Page must be 1 or more.");
        }

        var now = clock.UtcNow;

        // One row more than a page tells whether another page follows
        var criteria = new NcrListCriteria(
            FiltersFor(query, now), query.SortBy, query.Direction, Skip: (query.Page - 1) * PageSize, Take: PageSize + 1);
        var ncrs = await repository.ListAsync(criteria, cancellationToken);

        var items = ncrs.Take(PageSize).Select(ncr => ToSummary(ncr, now)).ToList();
        return new NcrListPage(items, query.Page, HasNextPage: ncrs.Count > PageSize);
    }

    private List<Expression<Func<NonConformance, bool>>> FiltersFor(ListNcrsQuery query, DateTimeOffset now)
    {
        List<Expression<Func<NonConformance, bool>>> filters = [currentUser.VisibleNcrs()];

        if (query.Status is { } status)
        {
            filters.Add(ncr => ncr.Status == status);
        }

        if (query.Severity is { } severity)
        {
            filters.Add(ncr => ncr.Details.Severity == severity);
        }

        if (query.OverdueOnly)
        {
            filters.Add(NonConformance.OverdueAt(now));
        }

        return filters;
    }

    private static NcrSummaryView ToSummary(NonConformance ncr, DateTimeOffset now) =>
        new(
            ncr.Id,
            ncr.Number.Value,
            ncr.Details.Title,
            ncr.Details.Severity,
            ncr.Status,
            ncr.Raised.At,
            ncr.DueDate,
            ncr.IsOverdue(now));
}
