using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using QualiTrack.Api.Authentication;

namespace QualiTrack.Api.IntegrationTests.Authentication;

[Collection(ApiCollection.Name)]
public sealed class PoliciesTests(QualiTrackApiFactory api)
{
    [Theory]
    [InlineData("Inspector")]
    [InlineData("Investigator")]
    [InlineData("QaManager")]
    public async Task A_signed_in_user_with_one_of_the_three_roles_meets_the_signed_in_policy(string role)
    {
        var result = await AuthorizeAsync(SignedIn(role));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task A_signed_in_user_without_a_role_does_not_meet_the_signed_in_policy()
    {
        var result = await AuthorizeAsync(SignedIn("Auditor"));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task A_signed_out_caller_does_not_meet_the_signed_in_policy()
    {
        var result = await AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.False(result.Succeeded);
    }

    private static ClaimsPrincipal SignedIn(string role) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], IdentityConstants.ApplicationScheme));

    private Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user) =>
        api.Services.GetRequiredService<IAuthorizationService>().AuthorizeAsync(user, Policies.SignedIn);
}
