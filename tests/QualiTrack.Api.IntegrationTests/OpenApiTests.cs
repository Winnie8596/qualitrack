using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QualiTrack.Api.IntegrationTests;

/// <summary>Phase 4's "Done when": the OpenAPI document (served in Development) lists every endpoint.</summary>
[Collection(ApiCollection.Name)]
public sealed class OpenApiTests(QualiTrackApiFactory api)
{
    public static TheoryData<string, string> Endpoints => new()
    {
        { "/api/auth/login", "post" },
        { "/api/auth/logout", "post" },
        { "/api/auth/me", "get" },
        { "/api/ncrs", "get" },
        { "/api/ncrs", "post" },
        { "/api/ncrs/{id}", "get" },
        { "/api/ncrs/{id}/assign", "post" },
        { "/api/ncrs/{id}/cancel", "post" },
        { "/api/ncrs/{id}/plan-fix", "post" },
        { "/api/ncrs/{id}/actions", "post" },
        { "/api/ncrs/{id}/actions/{actionId}/complete", "post" },
        { "/api/ncrs/{id}/submit", "post" },
        { "/api/ncrs/{id}/verify", "post" },
        { "/api/ncrs/{id}/reject", "post" },
        { "/api/ncrs/{id}/photo", "post" },
        { "/api/ncrs/{id}/photo", "get" },
        { "/api/users/investigators", "get" },
        { "/api/dashboard", "get" },
    };

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task The_openapi_document_lists_the_endpoint(string path, string method)
    {
        var client = api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        Assert.True(
            document.GetProperty("paths").TryGetProperty(path, out var operations) && operations.TryGetProperty(method, out _),
            $"{method.ToUpperInvariant()} {path} is not in the OpenAPI document");
    }
}
