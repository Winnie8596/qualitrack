namespace QualiTrack.Domain;

internal static class RequiredText
{
    /// <summary>Rejects blank or whitespace-only text with a message naming the field.</summary>
    public static string Of(string value, string fieldName) =>
        string.IsNullOrWhiteSpace(value) ? throw new DomainException($"{fieldName} is required.") : value;
}
