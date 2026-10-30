using QualiTrack.Domain;

namespace QualiTrack.Application.Abstractions;

/// <summary>Looks up users other than the caller, e.g. to check an assignee's role (A2).</summary>
public interface IUserDirectory
{
    /// <returns>The user's role, or <c>null</c> when no user has this id or they do not have exactly one role.</returns>
    Task<Role?> FindRoleAsync(UserId user, CancellationToken cancellationToken);
}
