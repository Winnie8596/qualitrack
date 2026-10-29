using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using QualiTrack.Application.Abstractions;
using QualiTrack.Domain;
using QualiTrack.Infrastructure.Identity;

namespace QualiTrack.Api.Authentication;

/// <summary>
/// The user signed in to the current request, read from the claims Identity put in the session cookie.
/// Lives in Api, not Infrastructure, because it needs the request. Only for endpoints that require authorization:
/// asking when nobody is signed in is a programming error, so it throws rather than answering with a guess.
/// </summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor, IOptions<IdentityOptions> identityOptions)
    : ICurrentUser
{
    private readonly ClaimsIdentityOptions _claimTypes = identityOptions.Value.ClaimsIdentity;

    public UserId Id =>
        Guid.TryParse(SignedInUser().FindFirstValue(_claimTypes.UserIdClaimType), out var id)
            ? new UserId(id)
            : throw new InvalidOperationException("The signed-in user has no valid user id claim.");

    // The role is the one at sign-in; a role changed since takes effect when the user signs in again
    public Role Role =>
        IdentityRoles.From(SignedInUser().FindAll(_claimTypes.RoleClaimType).Select(claim => claim.Value))
        ?? throw new InvalidOperationException("The signed-in user has none of the three roles.");

    private ClaimsPrincipal SignedInUser()
    {
        var user = accessor.HttpContext?.User;
        return user?.Identity?.IsAuthenticated == true
            ? user
            : throw new InvalidOperationException("No user is signed in; does the endpoint require authorization?");
    }
}
