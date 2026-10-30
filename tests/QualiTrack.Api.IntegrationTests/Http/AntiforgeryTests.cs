using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using QualiTrack.Api.Http;

namespace QualiTrack.Api.IntegrationTests.Http;

[Collection(ApiCollection.Name)]
public sealed class AntiforgeryTests(QualiTrackApiFactory api)
{
    [Fact]
    public async Task A_write_with_the_token_of_its_own_session_is_accepted()
    {
        var client = await SignedInAsync("inspector@qualitrack.demo");
        client.DefaultRequestHeaders.Add(AntiforgeryProtection.HeaderName, await TokenAsync(client));

        var response = await client.PostAsJsonAsync("/api/ncrs", Request(Guid.NewGuid().ToString()));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task A_write_without_the_token_is_rejected_with_400_and_changes_nothing()
    {
        var client = await SignedInAsync("inspector@qualitrack.demo");
        var marker = Guid.NewGuid().ToString();

        var response = await client.PostAsJsonAsync("/api/ncrs", Request(marker));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, await CountNcrsDescribedAsAsync(marker));
    }

    [Fact]
    public async Task A_write_with_a_token_from_another_session_is_rejected()
    {
        // The other session is another user's, as an attacker's own token would be
        var other = await SignedInAsync("qamanager@qualitrack.demo");
        var client = await SignedInAsync("inspector@qualitrack.demo");
        client.DefaultRequestHeaders.Add(AntiforgeryProtection.HeaderName, await TokenAsync(other));

        var response = await client.PostAsJsonAsync("/api/ncrs", Request(Guid.NewGuid().ToString()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_read_needs_no_token()
    {
        var client = await SignedInAsync("qamanager@qualitrack.demo");

        var response = await client.GetAsync($"/api/ncrs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Asking_for_a_token_without_signing_in_gets_401()
    {
        var response = await Client().GetAsync("/api/auth/antiforgery");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // The session and antiforgery cookies are Secure, so the client only sends them back over HTTPS
    private HttpClient Client() =>
        api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new("https://localhost") });

    private async Task<HttpClient> SignedInAsync(string email)
    {
        var client = Client();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = QualiTrackApiFactory.DemoPassword });
        login.EnsureSuccessStatusCode();
        return client;
    }

    private static async Task<string> TokenAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<AntiforgeryToken>("/api/auth/antiforgery");
        return response!.Token;
    }

    private static object Request(string description) =>
        new { title = "Burr on flange", description, location = "Line 3", severity = "Major" };

    private async Task<int> CountNcrsDescribedAsAsync(string description)
    {
        await using var context = api.CreateDbContext();
        return await context.NonConformances.CountAsync(ncr => ncr.Details.Description == description);
    }

    private sealed record AntiforgeryToken(string Token);
}
