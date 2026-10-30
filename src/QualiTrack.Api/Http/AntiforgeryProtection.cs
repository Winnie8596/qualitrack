using Microsoft.AspNetCore.Antiforgery;

namespace QualiTrack.Api.Http;

public sealed record AntiforgeryTokenResponse(string Token);

/// <summary>
/// CSRF defence for writes made with the session cookie, behind SameSite=Strict (ADR 0006). A page on another
/// site can make the browser send the cookie, but it cannot read the token, so it cannot put it in the header.
/// The token is tied to the signed-in user, so the web app fetches one after logging in.
/// </summary>
public static class AntiforgeryProtection
{
    public const string HeaderName = "X-XSRF-TOKEN";

    public static IServiceCollection AddAntiforgeryProtection(this IServiceCollection services) =>
        services.AddAntiforgery(options =>
        {
            options.HeaderName = HeaderName;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

    /// <summary>The token for the signed-in user; its cookie half is set on the same response.</summary>
    public static AntiforgeryTokenResponse IssueToken(HttpContext context, IAntiforgery antiforgery) =>
        new(antiforgery.GetAndStoreTokens(context).RequestToken
            ?? throw new InvalidOperationException("Antiforgery issued no request token."));

    /// <summary>
    /// Rejects a write to these endpoints without a valid token. The framework's own check only covers form
    /// posts, and this API takes JSON, so it is a filter here. Reads are let through: they change nothing.
    /// </summary>
    public static RouteGroupBuilder RequireAntiforgeryToken(this RouteGroupBuilder group) =>
        group.AddEndpointFilter(async (context, next) =>
        {
            var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            return await antiforgery.IsRequestValidAsync(context.HttpContext)
                ? await next(context)
                : TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "The antiforgery token is missing or invalid.");
        });
}
