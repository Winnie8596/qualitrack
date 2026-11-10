using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QualiTrack.Api.IntegrationTests.Http;

/// <summary>
/// The Docker image carries the built web app in the API's web root (plan.md Phase 6). Here a folder with a
/// stand-in build plays that part: a page, and one script named by its content hash as Vite names them.
/// </summary>
[Collection(ApiTestGroup.Name)]
public sealed class WebAppTests : IDisposable
{
    private const string Page = "<!doctype html><title>QualiTrack</title>";

    private readonly string _webRoot = Path.Combine(Path.GetTempPath(), $"qualitrack-web-{Guid.NewGuid():N}");
    private readonly WebApplicationFactory<Program> _withWebApp;
    private readonly HttpClient _client;
    private readonly HttpClient _apiOnly;

    public WebAppTests(QualiTrackApiFactory api)
    {
        Directory.CreateDirectory(Path.Combine(_webRoot, "assets"));
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), Page);
        File.WriteAllText(Path.Combine(_webRoot, "assets", "index-4f9a2c1b.js"), "console.log('QualiTrack')");

        var https = new WebApplicationFactoryClientOptions { BaseAddress = new("https://localhost") };
        _withWebApp = api.WithWebHostBuilder(builder => builder.UseWebRoot(_webRoot));
        _client = _withWebApp.CreateClient(https);
        _apiOnly = api.CreateClient(https);
    }

    public void Dispose()
    {
        _withWebApp.Dispose();
        Directory.Delete(_webRoot, recursive: true);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/ncrs")]
    [InlineData("/ncrs/7f1c1f0e-5d3b-4c57-9a55-0d6f6c1f2a10")]
    public async Task The_page_answers_every_address_the_web_app_routes_itself(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Page, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task The_page_is_checked_with_the_server_every_time_so_a_new_build_is_picked_up()
    {
        var response = await _client.GetAsync("/ncrs");

        Assert.Equal("no-cache", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task A_file_named_by_its_content_hash_may_be_kept_by_the_browser_for_a_year()
    {
        var response = await _client.GetAsync("/assets/index-4f9a2c1b.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/javascript", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("public, max-age=31536000, immutable", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task The_page_carries_the_security_headers()
    {
        var response = await _client.GetAsync("/");

        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.Equal("nosniff", string.Join(", ", response.Headers.GetValues("X-Content-Type-Options")));
    }

    [Theory]
    [InlineData("/api/no-such-route")]
    [InlineData("/assets/missing-00000000.js")]
    public async Task An_unknown_api_route_or_missing_file_is_still_not_found_rather_than_the_page(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task The_api_still_answers_alongside_the_page()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Without_a_built_web_app_there_is_no_page_to_serve()
    {
        var response = await _apiOnly.GetAsync("/ncrs");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
