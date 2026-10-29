namespace QualiTrack.Domain;

/// <summary>Who did something and when. Passed in so the domain never reads the clock or the current user.</summary>
public sealed record AuditStamp(UserId By, DateTimeOffset At);
