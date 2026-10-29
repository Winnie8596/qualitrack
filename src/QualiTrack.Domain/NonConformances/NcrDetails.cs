namespace QualiTrack.Domain.NonConformances;

/// <summary>BR-1: what was found, where, and how bad. Blank or whitespace text is rejected.</summary>
public sealed record NcrDetails
{
    public NcrDetails(string title, string description, string location, Severity severity)
    {
        if (!Enum.IsDefined(severity))
        {
            throw new DomainException("Severity must be Minor, Major or Critical.");
        }

        Title = Required(title, "Title");
        Description = Required(description, "Description");
        Location = Required(location, "Location");
        Severity = severity;
    }

    public string Title { get; }

    public string Description { get; }

    public string Location { get; }

    public Severity Severity { get; }

    private static string Required(string value, string fieldName) =>
        string.IsNullOrWhiteSpace(value) ? throw new DomainException($"{fieldName} is required.") : value;
}
