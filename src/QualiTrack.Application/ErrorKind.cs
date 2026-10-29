namespace QualiTrack.Application;

/// <summary>Why a use case failed. The API maps each kind to one HTTP status code.</summary>
public enum ErrorKind
{
    Validation,
    NotFound,
    Forbidden,
    Conflict,
}
