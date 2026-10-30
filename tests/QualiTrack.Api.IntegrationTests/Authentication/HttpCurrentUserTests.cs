using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using QualiTrack.Application.Abstractions;

namespace QualiTrack.Api.IntegrationTests.Authentication;

[Collection(ApiCollection.Name)]
public sealed class HttpCurrentUserTests(QualiTrackApiFactory api)
{
    [Theory]
    [InlineData("inspector@qualitrack.demo", Role.Inspector)]
    [InlineData("investigator@qualitrack.demo", Role.Investigator)]
    [InlineData("qamanager@qualitrack.demo", Role.QaManager)]
    public async Task The_current_user_is_the_one_signed_in_with_their_role(string email, Role expected)
    {
        _ = api.Server;
        await using var scope = api.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = await users.FindByEmailAsync(email);
        Assert.NotNull(user);

        // The principal the sign-in manager writes into the session cookie, so the claims are the real ones
        var principal = await scope.ServiceProvider.GetRequiredService<SignInManager<IdentityUser<Guid>>>()
            .CreateUserPrincipalAsync(user);
        var currentUser = CurrentUserFor(scope, principal);

        Assert.Equal(user.Id, currentUser.Id.Value);
        Assert.Equal(expected, currentUser.Role);
    }

    [Fact]
    public async Task Asking_for_the_current_user_when_nobody_is_signed_in_is_a_programming_error()
    {
        _ = api.Server;
        await using var scope = api.Services.CreateAsyncScope();

        var currentUser = CurrentUserFor(scope, new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.Throws<InvalidOperationException>(() => currentUser.Id);
        Assert.Throws<InvalidOperationException>(() => currentUser.Role);
    }

    [Fact]
    public async Task A_signed_in_user_without_one_of_the_three_roles_is_rejected()
    {
        _ = api.Server;
        await using var scope = api.Services.CreateAsyncScope();
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new Claim(ClaimTypes.Role, "Auditor")],
            IdentityConstants.ApplicationScheme);

        var currentUser = CurrentUserFor(scope, new ClaimsPrincipal(identity));

        Assert.Throws<InvalidOperationException>(() => currentUser.Role);
    }

    private static ICurrentUser CurrentUserFor(AsyncServiceScope scope, ClaimsPrincipal principal)
    {
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
            new DefaultHttpContext { User = principal };
        return scope.ServiceProvider.GetRequiredService<ICurrentUser>();
    }
}
