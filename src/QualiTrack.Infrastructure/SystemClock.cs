using QualiTrack.Application.Abstractions;

namespace QualiTrack.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
