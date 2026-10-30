using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using QualiTrack.Application.Abstractions;
using QualiTrack.Domain;

namespace QualiTrack.Api.IntegrationTests.Authentication;

[Collection(ApiCollection.Name)]
public sealed class IdentityUserDirectoryTests(QualiTrackApiFactory api)
{
    [Theory]
    [InlineData("inspector@qualitrack.demo", Role.Inspector)]
    [InlineData("investigator@qualitrack.demo", Role.Investigator)]
    [InlineData("qamanager@qualitrack.demo", Role.QaManager)]
    public async Task The_directory_finds_the_role_of_a_stored_user(string email, Role expected)
    {
        var user = await DemoUserIdAsync(email);
        await using var scope = api.Services.CreateAsyncScope();

        var role = await scope.ServiceProvider.GetRequiredService<IUserDirectory>()
            .FindRoleAsync(user, CancellationToken.None);

        Assert.Equal(expected, role);
    }

    [Fact]
    public async Task The_directory_finds_no_role_for_an_unknown_user()
    {
        _ = api.Server;
        await using var scope = api.Services.CreateAsyncScope();

        var role = await scope.ServiceProvider.GetRequiredService<IUserDirectory>()
            .FindRoleAsync(new UserId(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(role);
    }

    [Fact]
    public async Task The_directory_lists_the_users_with_a_role_by_id_and_email()
    {
        var investigator = await DemoUserIdAsync("investigator@qualitrack.demo");
        var inspector = await DemoUserIdAsync("inspector@qualitrack.demo");
        await using var scope = api.Services.CreateAsyncScope();

        var investigators = await scope.ServiceProvider.GetRequiredService<IUserDirectory>()
            .ListAsync(Role.Investigator, CancellationToken.None);

        Assert.Contains(new DirectoryUser(investigator, "investigator@qualitrack.demo"), investigators);
        Assert.DoesNotContain(investigators, user => user.Id == inspector);
    }

    private async Task<UserId> DemoUserIdAsync(string email)
    {
        _ = api.Server;
        await using var scope = api.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = await users.FindByEmailAsync(email);
        Assert.NotNull(user);
        return new UserId(user.Id);
    }
}
