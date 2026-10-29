using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QualiTrack.Api.IntegrationTests.Http;

[Collection(ApiCollection.Name)]
public sealed class ProblemResponseTests(QualiTrackApiFactory api)
{
    [Theory]
    [InlineData("/api/auth/me", HttpStatusCode.Unauthorized)]
    [InlineData("/api/no-such-route", HttpStatusCode.NotFound)]
    public async Task An_error_without_a_body_of_its_own_is_answered_as_a_problem(string path, HttpStatusCode status)
    {
        var client = api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new("https://localhost") });

        var response = await client.GetAsync(path);

        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal((int)status, problem?.Status);
    }
}
