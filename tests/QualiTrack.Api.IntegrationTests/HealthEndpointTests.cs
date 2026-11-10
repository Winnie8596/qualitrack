using System.Net;

namespace QualiTrack.Api.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class HealthEndpointTests(QualiTrackApiFactory api)
{
    [Fact]
    public async Task Health_returns_200_when_the_app_starts()
    {
        var client = api.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
