using QualiTrack.Application.Abstractions;
using QualiTrack.Domain;

namespace QualiTrack.Application.Tests.Fakes;

internal sealed class InMemoryUserDirectory : IUserDirectory
{
    private readonly Dictionary<UserId, Role> _roles = [];

    public UserId Add(Role role)
    {
        var user = new UserId(Guid.NewGuid());
        _roles.Add(user, role);
        return user;
    }

    public Task<Role?> FindRoleAsync(UserId user, CancellationToken cancellationToken) =>
        Task.FromResult(_roles.TryGetValue(user, out var role) ? role : (Role?)null);
}
