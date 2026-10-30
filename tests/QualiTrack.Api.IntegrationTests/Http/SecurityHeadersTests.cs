using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using QualiTrack.Application.Abstractions;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Api.IntegrationTests.Http;

[Collection(ApiCollection.Name)]
public sealed class SecurityHeadersTests(QualiTrackApiFactory api)
{
    [Theory]
    [InlineData("/health", HttpStatusCode.OK)]
    [InlineData("/api/auth/me", HttpStatusCode.Unauthorized)]
    [InlineData("/api/no-such-route", HttpStatusCode.NotFound)]
    public async Task Every_response_carries_the_security_headers(string path, HttpStatusCode status)
    {
        var response = await Client(api).GetAsync(path);

        Assert.Equal(status, response.StatusCode);
        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task An_unhandled_exception_still_carries_the_security_headers()
    {
        // The exception handler clears the response's headers before it writes the 500, so this is the case
        // that shows the headers are added as the response starts, not before the endpoint runs
        using var failing = api.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddScoped<INcrRepository, FailingNcrRepository>()));
        var client = Client(failing);
        var login = await client.PostAsJsonAsync(
            "/api/auth/login", new { email = "qamanager@qualitrack.demo", password = QualiTrackApiFactory.DemoPassword });
        login.EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/api/ncrs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        AssertSecurityHeaders(response);
    }

    private static HttpClient Client(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new("https://localhost") });

    private static void AssertSecurityHeaders(HttpResponseMessage response)
    {
        Assert.Equal(
            "default-src 'self'; object-src 'none'; base-uri 'none'; form-action 'self'; frame-ancestors 'none'",
            Header(response, "Content-Security-Policy"));
        Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
        Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
    }

    private static string? Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? string.Join(", ", values) : null;

    private sealed class FailingNcrRepository : INcrRepository
    {
        public void Add(NonConformance ncr) => throw new InvalidOperationException("The database is down.");

        public Task<NonConformance?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The database is down.");
    }
}
