using QualiTrack.Application.Abstractions;

namespace QualiTrack.Application.Tests.Fakes;

internal sealed class FixedClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}
