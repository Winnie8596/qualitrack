namespace QualiTrack.Domain.NonConformances;

public sealed class NonConformance
{
    private static readonly TimeSpan _criticalMaxTimeToDue = TimeSpan.FromDays(7);

    private readonly List<CorrectiveAction> _actions = [];
    private readonly List<StatusChange> _history = [];

    private NonConformance(NcrNumber number, NcrDetails details, AuditStamp raised)
    {
        Number = number;
        Details = details;
        Raised = raised;
    }

    public Guid Id { get; } = Guid.NewGuid();

    public NcrNumber Number { get; }

    public NcrDetails Details { get; }

    public AuditStamp Raised { get; }

    public NcrStatus Status { get; private set; }

    public UserId? InvestigatorId { get; private set; }

    public DateTimeOffset? DueDate { get; private set; }

    public string? RootCause { get; private set; }

    public IReadOnlyList<CorrectiveAction> Actions => _actions;

    public IReadOnlyList<StatusChange> History => _history;

    public bool IsFinished => throw new NotImplementedException();

    public static NonConformance Raise(NcrNumber number, NcrDetails details, AuditStamp raised) =>
        new(number, details, raised) { Status = NcrStatus.Open };

    public void AssignInvestigator(UserId investigator, DateTimeOffset dueDate, AuditStamp stamp)
    {
        if (dueDate <= stamp.At)
        {
            throw new DomainException("Due date must be in the future.");
        }

        // BR-2
        if (Details.Severity == Severity.Critical && dueDate > Raised.At + _criticalMaxTimeToDue)
        {
            throw new DomainException("A critical NCR must be due within 7 days of being raised.");
        }

        InvestigatorId = investigator;
        DueDate = dueDate;
        Status = NcrStatus.Investigating;
    }

    public void PlanFix(string rootCause, IReadOnlyCollection<string> actionDescriptions, AuditStamp stamp)
    {
        EnsureStatus(NcrStatus.Investigating);
        EnsureIsInvestigator(stamp.By);
        var cause = RequiredText.Of(rootCause, "Root cause");
        if (actionDescriptions.Count == 0)
        {
            throw new DomainException("Planning the fix needs at least one corrective action.");
        }

        // Build every action before changing anything, so a bad description leaves the NCR untouched
        var actions = actionDescriptions.Select(description => new CorrectiveAction(description, stamp)).ToList();

        RootCause = cause;
        _actions.AddRange(actions);
        Status = NcrStatus.ActionInProgress;
    }

    public void AddCorrectiveAction(string description, AuditStamp stamp) => throw new NotImplementedException();

    public void CompleteAction(Guid actionId, AuditStamp stamp) => throw new NotImplementedException();

    public void SubmitForVerification(AuditStamp stamp) => throw new NotImplementedException();

    public void Verify(AuditStamp stamp) => throw new NotImplementedException();

    public void RejectVerification(string reason, AuditStamp stamp) => throw new NotImplementedException();

    public void Cancel(string reason, AuditStamp stamp) => throw new NotImplementedException();

    public bool IsOverdue(DateTimeOffset now) => throw new NotImplementedException();

    private void EnsureStatus(NcrStatus required)
    {
        if (Status != required)
        {
            throw new DomainException($"NCR must be {required} but is {Status}.");
        }
    }

    // BR-3
    private void EnsureIsInvestigator(UserId user)
    {
        if (user != InvestigatorId)
        {
            throw new DomainException("Only the assigned investigator can do this.");
        }
    }
}
