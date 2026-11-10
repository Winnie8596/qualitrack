using Microsoft.AspNetCore.Authorization;
using QualiTrack.Application.Abstractions;

namespace QualiTrack.Api.Authentication;

/// <summary>
/// Who may reach an endpoint at all. Which role may call each use case is decided by its handler, so a wrong
/// role still gets the handler's 403 and its message; the policy only keeps out callers with no session or no role.
/// </summary>
public static class Policies
{
    public const string SignedIn = nameof(SignedIn);

    public static AuthorizationBuilder AddPolicies(this AuthorizationBuilder authorization) =>
        authorization.AddPolicy(SignedIn, policy => policy.RequireAuthenticatedUser().RequireRole(Enum.GetNames<Role>()));
}
