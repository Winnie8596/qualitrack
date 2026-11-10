using QualiTrack.Domain;

namespace QualiTrack.Application.Abstractions;

public interface ICurrentUser
{
    UserId Id { get; }

    Role Role { get; }
}
