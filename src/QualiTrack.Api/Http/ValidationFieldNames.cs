using System.Text.Json;

namespace QualiTrack.Api.Http;

/// <summary>
/// Validation names a failed field by its C# property ("Title"), but the request body names it in camelCase
/// ("title"). Renaming the keys of every validation problem lets the web app put each error on the field it sent.
/// </summary>
public static class ValidationFieldNames
{
    public static void UseJsonNames(ProblemDetailsContext context)
    {
        if (context.ProblemDetails is not HttpValidationProblemDetails problem)
        {
            return;
        }

        var errors = problem.Errors.ToDictionary(error => JsonNamingPolicy.CamelCase.ConvertName(error.Key), error => error.Value);
        problem.Errors.Clear();
        foreach (var (field, messages) in errors)
        {
            problem.Errors[field] = messages;
        }
    }
}
