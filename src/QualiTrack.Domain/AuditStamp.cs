namespace QualiTrack.Domain;

/// <summary>Who did something and when. Passed in so the domain never reads the clock or the current user.</summary>
public sealed record AuditStamp
{
    public AuditStamp(UserId by, DateTimeOffset at)
    {
        By = by;
        At = RequiredUtc.Of(at, "Audit time");
    }

    public UserId By { get; }

    public DateTimeOffset At { get; }
}
