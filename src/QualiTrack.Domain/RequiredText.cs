namespace QualiTrack.Domain;

internal static class RequiredText
{
    /// <summary>Rejects blank or whitespace-only text with a message naming the field.</summary>
    public static string Of(string value, string fieldName) =>
        string.IsNullOrWhiteSpace(value) ? throw DomainException.InvalidInput($"{fieldName} is required.") : value;
}
