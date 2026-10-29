using QualiTrack.Application.Abstractions;
using QualiTrack.Domain;

namespace QualiTrack.Application.Tests.Fakes;

internal sealed class FakeCurrentUser(UserId id, Role role) : ICurrentUser
{
    public FakeCurrentUser(Role role)
        : this(new UserId(Guid.NewGuid()), role)
    {
    }

    public UserId Id { get; } = id;

    public Role Role { get; } = role;
}
