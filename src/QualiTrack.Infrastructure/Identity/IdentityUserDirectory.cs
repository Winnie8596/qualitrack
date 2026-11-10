using Microsoft.EntityFrameworkCore;
using QualiTrack.Application.Abstractions;
using QualiTrack.Domain;
using QualiTrack.Infrastructure.Persistence;

namespace QualiTrack.Infrastructure.Identity;

// Reads Identity's tables directly: one query, where UserManager would take one to find the user and one for roles
internal sealed class IdentityUserDirectory(QualiTrackDbContext context) : IUserDirectory
{
    public async Task<Role?> FindRoleAsync(UserId user, CancellationToken cancellationToken)
    {
        var roleNames = await (
                from userRole in context.UserRoles
                join role in context.Roles on userRole.RoleId equals role.Id
                where userRole.UserId == user.Value
                select role.Name)
            .ToListAsync(cancellationToken);

        return IdentityRoles.From(roleNames);
    }

    // A user with more than one role has no role the app can act on (IdentityRoles.From), so is never listed
    public async Task<IReadOnlyList<DirectoryUser>> ListAsync(Role role, CancellationToken cancellationToken)
    {
        var roleName = role.ToString();
        var users = await context.Users
            .Where(user => user.Email != null
                && context.UserRoles.Count(userRole => userRole.UserId == user.Id) == 1
                && context.UserRoles.Any(userRole => userRole.UserId == user.Id
                    && context.Roles.Any(candidate => candidate.Id == userRole.RoleId && candidate.Name == roleName)))
            .Select(user => new { user.Id, Email = user.Email ?? string.Empty })
            .ToListAsync(cancellationToken);

        return [.. users.Select(user => new DirectoryUser(new UserId(user.Id), user.Email))];
    }
}
