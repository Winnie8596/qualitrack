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
}
