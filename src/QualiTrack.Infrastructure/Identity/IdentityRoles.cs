using QualiTrack.Application.Abstractions;

namespace QualiTrack.Infrastructure.Identity;

/// <summary>Reads a <see cref="Role"/> back from Identity role names, each named after its value (see RoleConfiguration).</summary>
public static class IdentityRoles
{
    /// <returns>
    /// The role among <paramref name="names"/>, or <c>null</c> unless exactly one of them is a <see cref="Role"/>:
    /// a user has exactly one role in v1 (PRD §4), and a user who breaks that has no role the app can act on.
    /// </returns>
    public static Role? From(IEnumerable<string?> names)
    {
        // Matched by name, not Enum.TryParse, which would also accept "1" or "inspector"
        var roles = Enum.GetValues<Role>().Where(role => names.Contains(role.ToString())).ToList();
        return roles.Count == 1 ? roles[0] : null;
    }
}
