using QualiTrack.Application.Abstractions;

namespace QualiTrack.Infrastructure.Identity;

/// <summary>Reads a <see cref="Role"/> back from Identity role names, each named after its value (see RoleConfiguration).</summary>
public static class IdentityRoles
{
    /// <returns>The one role among <paramref name="names"/>, or <c>null</c> when none of them is a <see cref="Role"/>.</returns>
    /// <exception cref="InvalidOperationException">More than one is: a user has exactly one role in v1 (PRD §4).</exception>
    public static Role? From(IEnumerable<string?> names)
    {
        // Matched by name, not Enum.TryParse, which would also accept "1" or "inspector"
        var roles = Enum.GetValues<Role>().Where(role => names.Contains(role.ToString())).ToList();
        return roles.Count switch
        {
            0 => null,
            1 => roles[0],
            _ => throw new InvalidOperationException($"A user has more than one role: {string.Join(", ", roles)}."),
        };
    }
}
