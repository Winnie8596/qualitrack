namespace QualiTrack.Domain.NonConformances;

/// <summary>One entry in an NCR's audit trail (BR-7). Created by the aggregate on every status change.</summary>
public sealed record StatusChange(NcrStatus From, NcrStatus To, AuditStamp Stamp, string? Note)
{
    // For EF Core only: it cannot pass a complex value (Stamp) to a constructor. "!" is safe because EF sets
    // every property from storage before anything reads them.
    private StatusChange()
        : this(default, default, null!, null)
    {
    }
}
