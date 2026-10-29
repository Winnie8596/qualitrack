namespace QualiTrack.Domain;

internal static class RequiredUtc
{
    /// <summary>All times are stored as UTC (PRD §8); converting to local time is the UI's job.</summary>
    public static DateTimeOffset Of(DateTimeOffset value, string fieldName) =>
        value.Offset == TimeSpan.Zero ? value : throw DomainException.InvalidInput($"{fieldName} must be in UTC.");
}
