namespace QualiTrack.Domain;

/// <summary>Thrown when a command would break a business rule. The entity is left unchanged.</summary>
public sealed class DomainException : Exception
{
    private DomainException(DomainErrorKind kind, string message)
        : base(message) => Kind = kind;

    public DomainErrorKind Kind { get; }

    public static DomainException InvalidInput(string message) => new(DomainErrorKind.InvalidInput, message);

    public static DomainException InvalidState(string message) => new(DomainErrorKind.InvalidState, message);

    public static DomainException NotPermitted(string message) => new(DomainErrorKind.NotPermitted, message);
}
