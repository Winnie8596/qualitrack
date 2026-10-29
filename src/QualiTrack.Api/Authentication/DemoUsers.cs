using Microsoft.AspNetCore.Identity;
using QualiTrack.Application.Abstractions;

namespace QualiTrack.Api.Authentication;

/// <summary>One user per role, so every step of the lifecycle can be tried locally. Development only; see Program.</summary>
public static class DemoUsers
{
    private const string Section = "DemoUsers";

    public static async Task SeedDemoUsersAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();

        // Every password is read before any user is created, so a missing one fails startup without a half-done seed
        var passwords = Enum.GetValues<Role>().Select(role => (role, RequiredPassword(configuration, role))).ToList();

        foreach (var (role, password) in passwords)
        {
            await EnsureUserAsync(users, role, password);
        }
    }

    // Not in appsettings: they are passwords. Set DemoUsers__<Role> in the environment or user secrets.
    private static string RequiredPassword(IConfiguration configuration, Role role)
    {
        var key = $"{Section}:{role}";
        var password = configuration[key];
        return string.IsNullOrWhiteSpace(password)
            ? throw new InvalidOperationException($"Demo user password '{key}' is not configured.")
            : password;
    }

    // An existing user is left alone, so seeding on every start keeps any password changed since
    private static async Task EnsureUserAsync(UserManager<IdentityUser<Guid>> users, Role role, string password)
    {
        var email = $"{role.ToString().ToLowerInvariant()}@qualitrack.demo";
        if (await users.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new IdentityUser<Guid> { UserName = email, Email = email, EmailConfirmed = true };
        ThrowIfFailed(await users.CreateAsync(user, password), email);
        ThrowIfFailed(await users.AddToRoleAsync(user, role.ToString()), email);
    }

    private static void ThrowIfFailed(IdentityResult result, string email)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Could not seed demo user '{email}': {errors}");
        }
    }
}
