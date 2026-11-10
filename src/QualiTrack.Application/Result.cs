namespace QualiTrack.Application;

/// <summary>
/// Outcome of a use case. Expected failures (not found, forbidden, rule broken) are returned, not thrown,
/// so callers must handle them and the API can map each <see cref="ErrorKind"/> to a status code.
/// </summary>
public class Result
{
    private readonly UseCaseError? _error;

    protected Result(UseCaseError? error) => _error = error;

    public bool IsSuccess => _error is null;

    public UseCaseError Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result Success() => new(null);

    public static Result Failure(UseCaseError error) => new(error);

    public static implicit operator Result(UseCaseError error) => Failure(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, UseCaseError? error)
        : base(error) => _value = value;

    // "!" is safe: _value is only default when the result failed, and that branch throws instead
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("A failed result has no value.");

    // A handler returns the value or the error itself and these conversions wrap it, so there is no
    // Result<Guid>.Success(id) to spell out at every return
    public static implicit operator Result<T>(T value) => new(value, null);

    public static implicit operator Result<T>(UseCaseError error) => new(default, error);
}
