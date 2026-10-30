using QualiTrack.Application.Abstractions;
using QualiTrack.Domain;

namespace QualiTrack.Application.Tests.Fakes;

internal sealed class InMemoryUserDirectory : IUserDirectory
{
    private readonly Dictionary<UserId, (Role Role, string Email)> _users = [];

    public UserId Add(Role role, string? email = null)
    {
        var user = new UserId(Guid.NewGuid());
        _users.Add(user, (role, email ?? $"{user.Value}@example.com"));
        return user;
    }

    public Task<Role?> FindRoleAsync(UserId user, CancellationToken cancellationToken) =>
        Task.FromResult(_users.TryGetValue(user, out var found) ? found.Role : (Role?)null);

    public Task<IReadOnlyList<DirectoryUser>> ListAsync(Role role, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DirectoryUser>>(
            [.. _users.Where(user => user.Value.Role == role).Select(user => new DirectoryUser(user.Key, user.Value.Email))]);
}
