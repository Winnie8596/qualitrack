using QualiTrack.Infrastructure;

namespace QualiTrack.Api.IntegrationTests;

public sealed class SystemClockTests
{
    [Fact]
    public void The_system_clock_tells_the_current_time_in_utc()
    {
        var before = DateTimeOffset.UtcNow;

        var now = new SystemClock().UtcNow;

        Assert.Equal(TimeSpan.Zero, now.Offset);
        Assert.InRange(now, before, DateTimeOffset.UtcNow);
    }
}
