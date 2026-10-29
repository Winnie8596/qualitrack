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

        Title = RequiredText.Of(title, "Title");
        Description = RequiredText.Of(description, "Description");
        Location = RequiredText.Of(location, "Location");
        Severity = severity;
    }

    public string Title { get; }

    public string Description { get; }

    public string Location { get; }

    public Severity Severity { get; }
}
