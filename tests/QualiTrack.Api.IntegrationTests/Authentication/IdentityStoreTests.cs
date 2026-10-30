using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace QualiTrack.Api.IntegrationTests.Authentication;

[Collection(ApiCollection.Name)]
public sealed class IdentityStoreTests(QualiTrackApiFactory api)
{
    [Fact]
    public async Task A_user_and_their_role_are_stored_in_postgresql()
    {
        var email = $"{Guid.NewGuid()}@qualitrack.test";
        var role = $"Role-{Guid.NewGuid()}";
        await using (var scope = api.Services.CreateAsyncScope())
        {
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
            var user = new IdentityUser<Guid> { UserName = email, Email = email };
            AssertSucceeded(await roles.CreateAsync(new IdentityRole<Guid>(role)));
            AssertSucceeded(await users.CreateAsync(user, "Correct-Horse-9"));
            AssertSucceeded(await users.AddToRoleAsync(user, role));
        }

        await using var readScope = api.Services.CreateAsyncScope();
        var readUsers = readScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var loaded = await readUsers.FindByEmailAsync(email);

        Assert.NotNull(loaded);
        Assert.True(await readUsers.CheckPasswordAsync(loaded, "Correct-Horse-9"));
        Assert.Equal([role], await readUsers.GetRolesAsync(loaded));
    }

    private static void AssertSucceeded(IdentityResult result) =>
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
}
