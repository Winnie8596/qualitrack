using QualiTrack.Application.Abstractions;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.NonConformances;

public sealed record GetNcrByIdQuery(Guid Id);

/// <summary>
/// Everything the detail page shows (D2), as plain data: callers get a copy they cannot use to change the NCR.
/// Users are ids; the API or web app turns them into names.
/// </summary>
public sealed record NcrView(
    Guid Id,
    string Number,
    string Title,
    string Description,
    string Location,
    Severity Severity,
    NcrStatus Status,
    Guid RaisedBy,
    DateTimeOffset RaisedAt,
    Guid? InvestigatorId,
    DateTimeOffset? DueDate,
    bool IsOverdue,
    string? RootCause,
    IReadOnlyList<CorrectiveActionView> Actions,
    IReadOnlyList<StatusChangeView> History,
    bool HasPhoto);

public sealed record CorrectiveActionView(
    Guid Id, string Description, Guid AddedBy, DateTimeOffset AddedAt, Guid? CompletedBy, DateTimeOffset? CompletedAt);

public sealed record StatusChangeView(NcrStatus From, NcrStatus To, Guid By, DateTimeOffset At, string? Note);

public sealed class GetNcrByIdHandler(INcrRepository repository, IClock clock, ICurrentUser currentUser)
{
    public async Task<Result<NcrView>> HandleAsync(GetNcrByIdQuery query, CancellationToken cancellationToken)
    {
        var ncr = await repository.GetByIdAsync(query.Id, cancellationToken);

        // An NCR the caller may not see gets the same answer as one that does not exist, so ids cannot be probed
        if (ncr is null || !currentUser.CanView(ncr))
        {
            return Error.NotFound($"NCR {query.Id} was not found.");
        }

        return ToView(ncr, clock.UtcNow);
    }

    private static NcrView ToView(NonConformance ncr, DateTimeOffset now) =>
        new(
            ncr.Id,
            ncr.Number.Value,
            ncr.Details.Title,
            ncr.Details.Description,
            ncr.Details.Location,
            ncr.Details.Severity,
            ncr.Status,
            ncr.Raised.By.Value,
            ncr.Raised.At,
            ncr.InvestigatorId?.Value,
            ncr.DueDate,
            ncr.IsOverdue(now),
            ncr.RootCause,
            [.. ncr.Actions.Select(ToView)],
            [.. ncr.History.Select(ToView)],
            ncr.Photo is not null);

    private static CorrectiveActionView ToView(CorrectiveAction action) =>
        new(
            action.Id,
            action.Description,
            action.Added.By.Value,
            action.Added.At,
            action.Completed?.By.Value,
            action.Completed?.At);

    private static StatusChangeView ToView(StatusChange change) =>
        new(change.From, change.To, change.Stamp.By.Value, change.Stamp.At, change.Note);
}
