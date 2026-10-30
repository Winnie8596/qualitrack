namespace QualiTrack.Api.Http;

/// <summary>
/// Headers that tell the browser to refuse what this app never does, so an injected script, a sniffed file type
/// or a framing page has less to work with (OWASP secure headers).
/// </summary>
public static class SecurityHeaders
{
    // Only this origin may supply scripts, styles and data, which suits the JSON API now and the built web app
    // it will serve later (Phase 6); no plugins, no <base> rewriting, and no page may frame this one (clickjacking)
    private const string ContentSecurityPolicy =
        "default-src 'self'; object-src 'none'; base-uri 'none'; form-action 'self'; frame-ancestors 'none'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            // Added as the response starts rather than now: the exception handler clears the headers before it
            // writes a 500, and those must carry them too
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.ContentSecurityPolicy = ContentSecurityPolicy;

                // Browsers use the declared Content-Type and never guess one, so JSON is not run as a script
                headers.XContentTypeOptions = "nosniff";

                // URLs hold NCR ids; no request to another site learns which page it came from
                headers["Referrer-Policy"] = "no-referrer";
                return Task.CompletedTask;
            });
            await next(context);
        });
}
