namespace QualiTrack.Application;

public sealed record UseCaseError(ErrorKind Kind, string Message)
{
    public static UseCaseError Validation(string message) => new(ErrorKind.Validation, message);

    public static UseCaseError NotFound(string message) => new(ErrorKind.NotFound, message);

    public static UseCaseError Forbidden(string message) => new(ErrorKind.Forbidden, message);

    public static UseCaseError Conflict(string message) => new(ErrorKind.Conflict, message);
}
