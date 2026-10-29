using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using QualiTrack.Application.Abstractions;
using QualiTrack.Infrastructure.Identity;

namespace QualiTrack.Api.Authentication;

public sealed record LoginRequest(string? Email, string? Password);

/// <summary>Who is signed in. The role is a name, so the web app does not depend on the enum's numbering.</summary>
public sealed record CurrentUserResponse(Guid Id, string Email, string Role);

/// <summary>Login, logout and "who am I" for the React app, over the session cookie (PRD E1).</summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/auth");
        auth.MapPost("/login", LoginAsync).RequireRateLimiting(LoginRateLimit.Policy);
        auth.MapPost("/logout", LogoutAsync);
        auth.MapGet("/me", Me).RequireAuthorization(Policies.SignedIn);
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request, UserManager<IdentityUser<Guid>> users, SignInManager<IdentityUser<Guid>> signIn)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["credentials"] = ["Email and password are required."],
            });
        }

        // An unknown email, a wrong password and a locked-out account all get the same answer, so a caller
        // cannot use the login to find out which emails have accounts
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || !(await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true)).Succeeded)
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Email or password is wrong.");
        }

        // Checked before the cookie is issued, so every signed-in user has a role ICurrentUser can read
        if (IdentityRoles.From(await users.GetRolesAsync(user)) is not { } role)
        {
            return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "This account has no role.");
        }

        await signIn.SignInAsync(user, isPersistent: false);
        return Results.Ok(new CurrentUserResponse(user.Id, user.Email ?? request.Email, role.ToString()));
    }

    // Open to anyone: logging out when already logged out has nothing to refuse
    private static async Task<IResult> LogoutAsync(SignInManager<IdentityUser<Guid>> signIn)
    {
        await signIn.SignOutAsync();
        return Results.NoContent();
    }

    // What the server will authorise with: the user and role from the session cookie, not a fresh lookup
    private static CurrentUserResponse Me(ICurrentUser currentUser, ClaimsPrincipal principal) =>
        new(currentUser.Id.Value, principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty, currentUser.Role.ToString());
}
