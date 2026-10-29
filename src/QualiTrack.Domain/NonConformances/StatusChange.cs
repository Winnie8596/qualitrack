namespace QualiTrack.Domain.NonConformances;

/// <summary>One entry in an NCR's audit trail (BR-7). Created by the aggregate on every status change.</summary>
public sealed record StatusChange(NcrStatus From, NcrStatus To, AuditStamp Stamp, string? Note);
