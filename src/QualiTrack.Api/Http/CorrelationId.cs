using System.Text.RegularExpressions;

namespace QualiTrack.Api.Http;

/// <summary>
/// Gives every request an id that is on its response and on every log line written while handling it, so one
/// user's "it failed" can be traced through the logs. A caller (the web app, a proxy) may send its own.
/// </summary>
public static partial class CorrelationId
{
    public const string HeaderName = "X-Correlation-ID";

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var id = FromCaller(context.Request) ?? Guid.NewGuid().ToString("N");
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = id;
                return Task.CompletedTask;
            });

            // Scopes belong to the logger factory, not this logger, so every log line in the request carries the id
            var logger = context.RequestServices.GetRequiredService<ILogger<HttpContext>>();
            using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
            {
                await next(context);
            }
        });

    // Anything else is replaced: the id is written into logs, so it must not carry new lines or a wall of text
    private static string? FromCaller(HttpRequest request) =>
        request.Headers[HeaderName].ToString() is var sent && PlainId().IsMatch(sent) ? sent : null;

    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$")]
    private static partial Regex PlainId();
}
