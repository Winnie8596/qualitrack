using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using QualiTrack.Api.IntegrationTests.Ncrs;

namespace QualiTrack.Api.IntegrationTests.Authentication;

[Collection(ApiTestGroup.Name)]
public sealed class SessionKeysTests(QualiTrackApiFactory api) : IDisposable
{
    private readonly string _keys = Path.Combine(Path.GetTempPath(), $"qualitrack-keys-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_keys))
        {
            Directory.Delete(_keys, recursive: true);
        }
    }

    // The session cookie is encrypted with these keys. A container's own disk is lost when it is replaced, and
    // with it every session; kept on a volume, the next container can still read the cookies the last one issued.
    [Fact]
    public async Task The_keys_that_protect_the_session_cookie_are_kept_where_configuration_says()
    {
        using var host = api.WithWebHostBuilder(builder => builder.UseSetting("DataProtection:KeysPath", _keys));
        var client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new("https://localhost") });

        var login = await client.PostAsJsonAsync(
            "/api/auth/login", new { email = NcrApi.Inspector, password = QualiTrackApiFactory.DemoPassword });

        login.EnsureSuccessStatusCode();
        Assert.NotEmpty(Directory.GetFiles(_keys, "key-*.xml"));
    }
}
