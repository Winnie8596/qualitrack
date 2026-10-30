using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using QualiTrack.Api.Authentication;

namespace QualiTrack.Api.IntegrationTests.Authentication;

[Collection(ApiCollection.Name)]
public sealed class DemoUserSeedTests(QualiTrackApiFactory api)
{
    [Theory]
    [InlineData("inspector@qualitrack.demo", "Inspector")]
    [InlineData("investigator@qualitrack.demo", "Investigator")]
    [InlineData("qamanager@qualitrack.demo", "QaManager")]
    public async Task In_development_each_role_has_a_demo_user_with_the_configured_password(string email, string role)
    {
        _ = api.Server;
        await using var scope = api.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();

        var user = await users.FindByEmailAsync(email);

        Assert.NotNull(user);
        Assert.True(await users.CheckPasswordAsync(user, QualiTrackApiFactory.DemoPassword));
        Assert.Equal([role], await users.GetRolesAsync(user));
    }

    [Fact]
    public async Task Seeding_again_leaves_the_demo_users_as_they_are()
    {
        _ = api.Server;

        await api.Services.SeedDemoUsersAsync();

        await using var scope = api.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        Assert.Single(await users.GetUsersInRoleAsync("Inspector"), user => user.Email == "inspector@qualitrack.demo");
    }

    [Fact]
    public void Startup_fails_with_a_clear_message_when_a_demo_password_is_not_configured()
    {
        using var withoutPassword = api.WithWebHostBuilder(builder => builder.UseSetting("DemoUsers:QaManager", ""));

        var error = Assert.Throws<InvalidOperationException>(() => withoutPassword.Server);

        Assert.Contains("DemoUsers:QaManager", error.Message, StringComparison.Ordinal);
    }
}
