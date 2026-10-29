using QualiTrack.Domain;

namespace QualiTrack.Application;

/// <summary>The one place a broken domain rule becomes an application error, so every handler maps it the same way.</summary>
public static class DomainErrors
{
    public static Error From(DomainException exception) => exception.Kind switch
    {
        DomainErrorKind.InvalidInput => Error.Validation(exception.Message),
        DomainErrorKind.InvalidState => Error.Conflict(exception.Message),
        DomainErrorKind.NotPermitted => Error.Forbidden(exception.Message),
        _ => throw new ArgumentOutOfRangeException(nameof(exception), exception.Kind, "Unknown domain error kind."),
    };
}
