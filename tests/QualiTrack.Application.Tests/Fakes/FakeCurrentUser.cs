using QualiTrack.Application.Abstractions;
using QualiTrack.Domain;

namespace QualiTrack.Application.Tests.Fakes;

internal sealed class FakeCurrentUser(Role role) : ICurrentUser
{
    public UserId Id { get; } = new(Guid.NewGuid());

    public Role Role { get; } = role;
}
