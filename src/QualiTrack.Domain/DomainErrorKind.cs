namespace QualiTrack.Domain;

/// <summary>
/// Which kind of rule a command broke. The domain names it because only the domain knows;
/// the application layer turns it into a result the caller can act on.
/// </summary>
public enum DomainErrorKind
{
    /// <summary>A value supplied with the command is not acceptable (blank text, due date too late).</summary>
    InvalidInput,

    /// <summary>The command is not allowed in the NCR's current status.</summary>
    InvalidState,

    /// <summary>This user may not do this to this NCR (BR-3, BR-5).</summary>
    NotPermitted,
}
