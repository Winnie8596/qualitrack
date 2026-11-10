using Microsoft.Net.Http.Headers;

namespace QualiTrack.Api.Http;

/// <summary>
/// Serves the built React app from the web root, which the Docker image fills with <c>web/dist</c>. One origin
/// for page and API means no CORS, and the session cookie can stay SameSite=Strict. A build without the web app
/// (dotnet run beside the Vite dev server, the tests) serves the API alone.
/// </summary>
public static class WebApp
{
    private const string PageFile = "index.html";

    // Vite names every file under /assets by a hash of its content, so a changed file has a new address
    private const string HashedFilesPath = "/assets";
    private const string KeepForAYear = "public, max-age=31536000, immutable";

    // The page itself keeps its address, so the browser must ask each time whether it has changed
    private const string AskEveryTime = "no-cache";

    public static bool HasWebApp(this WebApplication app) =>
        app.Environment.WebRootFileProvider.GetFileInfo(PageFile).Exists;

    public static IApplicationBuilder UseWebAppFiles(this IApplicationBuilder app) =>
        app.UseStaticFiles(CacheHeaders());

    /// <summary>
    /// The React app routes addresses such as /ncrs/{id} itself, so any address that is not an endpoint or a file
    /// gets the page. An unknown /api address stays a 404: a caller expecting JSON must not get HTML.
    /// </summary>
    public static void MapWebAppFallback(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapFallback("/api/{**path}", () => Results.NotFound());
        endpoints.MapFallbackToFile(PageFile, CacheHeaders());
    }

    private static StaticFileOptions CacheHeaders() => new()
    {
        OnPrepareResponse = context =>
            context.Context.Response.Headers[HeaderNames.CacheControl] =
                context.Context.Request.Path.StartsWithSegments(HashedFilesPath, StringComparison.Ordinal)
                    ? KeepForAYear
                    : AskEveryTime,
    };
}
