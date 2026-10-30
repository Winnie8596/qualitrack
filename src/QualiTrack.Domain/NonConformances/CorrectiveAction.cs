namespace QualiTrack.Domain.NonConformances;

/// <summary>
/// One fix for an NCR. Created and completed only through the NonConformance aggregate,
/// which checks that the caller is the assigned investigator (BR-3).
/// </summary>
public sealed class CorrectiveAction
{
    internal CorrectiveAction(string description, AuditStamp added)
    {
        Description = RequiredText.Of(description, "Corrective action description");
        Added = added;
    }

    // For EF Core only: it cannot pass a complex value (Added) to a constructor. "!" is safe because EF sets
    // every property from storage before anything reads them.
    private CorrectiveAction()
    {
        Description = null!;
        Added = null!;
    }

    public Guid Id { get; } = Guid.NewGuid();

    public string Description { get; }

    public AuditStamp Added { get; }

    public AuditStamp? Completed { get; private set; }

    public bool IsCompleted => Completed is not null;

    internal void Complete(AuditStamp completed)
    {
        if (IsCompleted)
        {
            throw DomainException.InvalidState("Corrective action is already completed.");
        }

        Completed = completed;
    }
}
