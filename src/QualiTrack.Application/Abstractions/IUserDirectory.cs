using QualiTrack.Domain;

namespace QualiTrack.Application.Abstractions;

public sealed record DirectoryUser(UserId Id, string Email);

/// <summary>Looks up users other than the caller, e.g. to check an assignee's role (A2).</summary>
public interface IUserDirectory
{
    /// <returns>The user's role, or <c>null</c> when no user has this id or they do not have exactly one role.</returns>
    Task<Role?> FindRoleAsync(UserId user, CancellationToken cancellationToken);

    /// <returns>Every user whose one role is <paramref name="role"/>, in no particular order.</returns>
    Task<IReadOnlyList<DirectoryUser>> ListAsync(Role role, CancellationToken cancellationToken);
}
