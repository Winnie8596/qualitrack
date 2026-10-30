using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace QualiTrack.Api.IntegrationTests.Authentication;

[Collection(ApiCollection.Name)]
public sealed class AuthEndpointsTests(QualiTrackApiFactory api)
{
    [Theory]
    [InlineData("inspector@qualitrack.demo", "Inspector")]
    [InlineData("investigator@qualitrack.demo", "Investigator")]
    [InlineData("qamanager@qualitrack.demo", "QaManager")]
    public async Task A_user_who_logs_in_is_told_who_they_are_and_stays_signed_in(string email, string role)
    {
        var client = Client();

        var login = await LoginAsync(client, email, QualiTrackApiFactory.DemoPassword);
        var me = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var loggedIn = await login.Content.ReadFromJsonAsync<CurrentUser>();
        Assert.Equal(new CurrentUser(loggedIn!.Id, email, role), loggedIn);
        Assert.Equal(loggedIn, await me.Content.ReadFromJsonAsync<CurrentUser>());
    }

    [Fact]
    public async Task Asking_who_you_are_without_logging_in_gets_401()
    {
        var response = await Client().GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_wrong_password_gets_401_and_no_session()
    {
        // A user of its own: wrong passwords count towards lockout, which must not reach the shared demo users
        var email = await CreateUserAsync(role: "Inspector");
        var client = Client();

        var login = await LoginAsync(client, email, "Wrong-Password-1");

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task An_unknown_email_gets_the_same_401_as_a_wrong_password()
    {
        var login = await LoginAsync(Client(), $"{Guid.NewGuid()}@qualitrack.test", "Any-Password-1");

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task A_user_without_one_of_the_three_roles_cannot_log_in()
    {
        var email = await CreateUserAsync(role: null);
        var client = Client();

        var login = await LoginAsync(client, email, Password);

        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Theory]
    [InlineData("", "Any-Password-1")]
    [InlineData("inspector@qualitrack.demo", " ")]
    public async Task A_login_without_an_email_or_password_gets_400(string email, string password)
    {
        var login = await LoginAsync(Client(), email, password);

        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
    }

    [Fact]
    public async Task After_logging_out_the_session_is_gone()
    {
        var client = Client();
        await LoginAsync(client, "inspector@qualitrack.demo", QualiTrackApiFactory.DemoPassword);

        var logout = await client.PostAsync("/api/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Too_many_logins_in_a_short_time_get_429()
    {
        // A host of its own, so its small limit neither sees nor spends the other tests' logins
        using var limited = api.WithWebHostBuilder(builder => builder.UseSetting("LoginRateLimit:PermitLimit", "2"));
        var client = limited.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = _https });

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            statuses.Add((await LoginAsync(client, "unknown@qualitrack.test", "Any-Password-1")).StatusCode);
        }

        Assert.Equal([HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests], statuses);
    }

    private const string Password = "Correct-Horse-9";

    // The session cookie is Secure, so the client only sends it back over HTTPS
    private static readonly Uri _https = new("https://localhost");

    private HttpClient Client() => api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = _https });

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });

    private async Task<string> CreateUserAsync(string? role)
    {
        _ = api.Server;
        var email = $"{Guid.NewGuid()}@qualitrack.test";
        await using var scope = api.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = new IdentityUser<Guid> { UserName = email, Email = email };
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        if (role is not null)
        {
            Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        }

        return email;
    }

    private sealed record CurrentUser(Guid Id, string Email, string Role);
}
