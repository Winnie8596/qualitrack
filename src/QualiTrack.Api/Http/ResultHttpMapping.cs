using Microsoft.AspNetCore.Http.HttpResults;
using QualiTrack.Application;

namespace QualiTrack.Api.Http;

/// <summary>
/// The one place a use case's <see cref="Result"/> becomes an HTTP response (ADR 0005). Failures become RFC 9457
/// ProblemDetails with the status code of their <see cref="ErrorKind"/>; the message is the detail, never inspected.
/// </summary>
public static class ResultHttpMapping
{
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? TypedResults.NoContent() : Problem(result.Error);

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : Problem(result.Error);

    // The title is left to the framework, which names the status code ("Not Found") and links to its RFC section
    private static ProblemHttpResult Problem(Error error) =>
        TypedResults.Problem(statusCode: StatusCodeOf(error.Kind), detail: error.Message);

    private static int StatusCodeOf(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown error kind."),
    };
}
