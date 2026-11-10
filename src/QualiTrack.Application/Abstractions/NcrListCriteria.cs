using System.Linq.Expressions;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Abstractions;

public enum NcrSortField
{
    RaisedAt,
    DueDate,
}

public enum SortDirection
{
    Descending,
    Ascending,
}

/// <summary>
/// One page of the NCR list as the repository should fetch it. Every filter must hold. NCRs with no due date
/// come last when sorting by due date, in either direction.
/// </summary>
public sealed record NcrListCriteria(
    IReadOnlyList<Expression<Func<NonConformance, bool>>> Filters,
    NcrSortField SortBy,
    SortDirection Direction,
    int Skip,
    int Take);
