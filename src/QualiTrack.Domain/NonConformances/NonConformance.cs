using System.Linq.Expressions;

namespace QualiTrack.Domain.NonConformances;

public sealed class NonConformance
{
    private static readonly TimeSpan _criticalMaxTimeToDue = TimeSpan.FromDays(7);

    // The rules below are written once, as expressions, so an NCR can answer them in memory (IsFinished,
    // IsOverdue) and a query can filter on them in SQL (Unfinished, OverdueAt). Declared in this order on purpose:
    // static fields are set in the order they are written, and each rule is built from the one above it.
    private static readonly Expression<Func<NonConformance, bool>> _unfinishedRule =
        ncr => ncr.Status != NcrStatus.Closed && ncr.Status != NcrStatus.Cancelled;

    private static readonly Func<NonConformance, bool> _isUnfinished = _unfinishedRule.Compile();

    // BR-8. With no due date yet, the lifted comparison "now > null" is false, so it is not overdue.
    private static readonly Expression<Func<NonConformance, DateTimeOffset, bool>> _overdueRule =
        UnfinishedAnd((ncr, now) => now > ncr.DueDate);

    private static readonly Func<NonConformance, DateTimeOffset, bool> _isOverdue = _overdueRule.Compile();

    private readonly List<CorrectiveAction> _actions = [];
    private readonly List<StatusChange> _history = [];

    private NonConformance(NcrNumber number, NcrDetails details, AuditStamp raised)
    {
        Number = number;
        Details = details;
        Raised = raised;
    }

    // For EF Core only: it cannot pass complex values (Details, Raised) to a constructor, so it builds an empty
    // instance and sets every property from the row. "!" is safe because nothing reads them before that.
    private NonConformance()
    {
        Number = null!;
        Details = null!;
        Raised = null!;
    }

    public Guid Id { get; } = Guid.NewGuid();

    public NcrNumber Number { get; }

    public NcrDetails Details { get; }

    public AuditStamp Raised { get; }

    public NcrStatus Status { get; private set; }

    public UserId? InvestigatorId { get; private set; }

    public DateTimeOffset? DueDate { get; private set; }

    public string? RootCause { get; private set; }

    /// <summary>The format of the NCR's one optional photo (A1), or <c>null</c> when it has none.</summary>
    public PhotoFormat? Photo { get; private set; }

    public IReadOnlyList<CorrectiveAction> Actions => _actions;

    public IReadOnlyList<StatusChange> History => _history;

    /// <summary>Not closed or cancelled, as a filter a query provider such as EF Core can translate.</summary>
    public static Expression<Func<NonConformance, bool>> Unfinished => _unfinishedRule;

    // BR-6: a finished NCR cannot be changed in any way
    public bool IsFinished => !_isUnfinished(this);

    public static NonConformance Raise(NcrNumber number, NcrDetails details, AuditStamp raised) =>
        new(number, details, raised) { Status = NcrStatus.Open };

    public void AssignInvestigator(UserId investigator, DateTimeOffset dueDate, AuditStamp stamp)
    {
        EnsureStatus(NcrStatus.Open);
        RequiredUtc.Of(dueDate, "Due date");
        if (dueDate <= stamp.At)
        {
            throw DomainException.InvalidInput("Due date must be in the future.");
        }

        // BR-2
        if (Details.Severity == Severity.Critical && dueDate > Raised.At + _criticalMaxTimeToDue)
        {
            throw DomainException.InvalidInput($"A critical NCR must be due within {_criticalMaxTimeToDue.Days} days of being raised.");
        }

        InvestigatorId = investigator;
        DueDate = dueDate;
        MoveTo(NcrStatus.Investigating, stamp);
    }

    public void PlanFix(string rootCause, IReadOnlyCollection<string> actionDescriptions, AuditStamp stamp)
    {
        EnsureStatus(NcrStatus.Investigating);
        EnsureIsInvestigator(stamp.By);
        var cause = RequiredText.Of(rootCause, "Root cause");
        if (actionDescriptions.Count == 0)
        {
            throw DomainException.InvalidInput("Planning the fix needs at least one corrective action.");
        }

        // Build every action before changing anything, so a bad description leaves the NCR untouched
        var actions = actionDescriptions.Select(description => new CorrectiveAction(description, stamp)).ToList();

        RootCause = cause;
        _actions.AddRange(actions);
        MoveTo(NcrStatus.ActionInProgress, stamp);
    }

    public void AddCorrectiveAction(string description, AuditStamp stamp)
    {
        EnsureStatus(NcrStatus.ActionInProgress);
        EnsureIsInvestigator(stamp.By);

        _actions.Add(new CorrectiveAction(description, stamp));
    }

    public void CompleteAction(Guid actionId, AuditStamp stamp)
    {
        EnsureStatus(NcrStatus.ActionInProgress);
        EnsureIsInvestigator(stamp.By);
        var action = _actions.SingleOrDefault(candidate => candidate.Id == actionId)
            ?? throw DomainException.InvalidInput($"Corrective action {actionId} is not on this NCR.");

        action.Complete(stamp);
    }

    // BR-4. Root cause and at least one action are guaranteed by PlanFix, the only way into ActionInProgress.
    public void SubmitForVerification(AuditStamp stamp)
    {
        EnsureStatus(NcrStatus.ActionInProgress);
        EnsureIsInvestigator(stamp.By);
        if (_actions.Exists(action => !action.IsCompleted))
        {
            throw DomainException.InvalidState("Every corrective action must be completed before submitting for verification.");
        }

        MoveTo(NcrStatus.PendingVerification, stamp);
    }

    public void Verify(AuditStamp stamp)
    {
        EnsureStatus(NcrStatus.PendingVerification);
        EnsureIsNotInvestigator(stamp.By);

        MoveTo(NcrStatus.Closed, stamp);
    }

    public void RejectVerification(string reason, AuditStamp stamp)
    {
        EnsureStatus(NcrStatus.PendingVerification);
        EnsureIsNotInvestigator(stamp.By);
        var note = RequiredText.Of(reason, "Rejection reason");

        MoveTo(NcrStatus.ActionInProgress, stamp, note);
    }

    public void Cancel(string reason, AuditStamp stamp)
    {
        EnsureStatus(NcrStatus.Open);
        var note = RequiredText.Of(reason, "Cancellation reason");

        MoveTo(NcrStatus.Cancelled, stamp, note);
    }

    public bool IsOverdue(DateTimeOffset now) => _isOverdue(this, now);

    /// <summary>BR-8 as a filter a query provider such as EF Core can translate.</summary>
    public static Expression<Func<NonConformance, bool>> OverdueAt(DateTimeOffset now)
    {
        // Reading "now" from a closure, not a constant, lets EF Core send it as a SQL parameter
        Expression<Func<DateTimeOffset>> captured = () => now;
        var ncr = _overdueRule.Parameters[0];
        var body = new ParameterReplacer(_overdueRule.Parameters[1], captured.Body).Visit(_overdueRule.Body);
        return Expression.Lambda<Func<NonConformance, bool>>(body, ncr);
    }

    // BR-7: every change after Raise goes through here, so no transition can skip the audit trail
    private void MoveTo(NcrStatus to, AuditStamp stamp, string? note = null)
    {
        _history.Add(new StatusChange(Status, to, stamp, note));
        Status = to;
    }

    // A1. Not a status change, so not in the history. Only while open: it records what was found, not later work.
    public void AttachPhoto(PhotoFormat format, AuditStamp stamp)
    {
        EnsureStatus(NcrStatus.Open);
        if (stamp.By != Raised.By)
        {
            throw DomainException.NotPermitted("Only the inspector who raised the NCR can attach its photo.");
        }

        if (Photo is not null)
        {
            throw DomainException.InvalidState("An NCR has at most one photo.");
        }

        Photo = format;
    }

    private void EnsureStatus(NcrStatus required)
    {
        if (Status != required)
        {
            throw DomainException.InvalidState($"NCR must be {required} but is {Status}.");
        }
    }

    // BR-3
    private void EnsureIsInvestigator(UserId user)
    {
        if (user != InvestigatorId)
        {
            throw DomainException.NotPermitted("Only the assigned investigator can do this.");
        }
    }

    // BR-5
    private void EnsureIsNotInvestigator(UserId user)
    {
        if (user == InvestigatorId)
        {
            throw DomainException.NotPermitted("The investigator cannot verify their own fix.");
        }
    }

    private static Expression<Func<NonConformance, DateTimeOffset, bool>> UnfinishedAnd(
        Expression<Func<NonConformance, DateTimeOffset, bool>> condition)
    {
        var ncr = condition.Parameters[0];
        var unfinished = new ParameterReplacer(_unfinishedRule.Parameters[0], ncr).Visit(_unfinishedRule.Body);
        return Expression.Lambda<Func<NonConformance, DateTimeOffset, bool>>(
            Expression.AndAlso(unfinished, condition.Body), condition.Parameters);
    }

    private sealed class ParameterReplacer(ParameterExpression parameter, Expression replacement) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == parameter ? replacement : base.VisitParameter(node);
    }
}
