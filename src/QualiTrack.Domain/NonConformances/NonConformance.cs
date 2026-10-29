namespace QualiTrack.Domain.NonConformances;

public sealed class NonConformance
{
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

    public void AssignInvestigator(UserId investigator, DateTimeOffset dueDate, AuditStamp stamp) =>
        throw new NotImplementedException();

    public void PlanFix(string rootCause, IReadOnlyCollection<string> actionDescriptions, AuditStamp stamp) =>
        throw new NotImplementedException();

    public void AddCorrectiveAction(string description, AuditStamp stamp) => throw new NotImplementedException();

    public void CompleteAction(Guid actionId, AuditStamp stamp) => throw new NotImplementedException();

    public void SubmitForVerification(AuditStamp stamp) => throw new NotImplementedException();

    public void Verify(AuditStamp stamp) => throw new NotImplementedException();

    public void RejectVerification(string reason, AuditStamp stamp) => throw new NotImplementedException();

    public void Cancel(string reason, AuditStamp stamp) => throw new NotImplementedException();

    public bool IsOverdue(DateTimeOffset now) => throw new NotImplementedException();
}
