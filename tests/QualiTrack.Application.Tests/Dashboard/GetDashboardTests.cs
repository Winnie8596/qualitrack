using QualiTrack.Application.Abstractions;
using QualiTrack.Application.Dashboard;
using QualiTrack.Application.Tests.Fakes;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Tests.Dashboard;

public sealed class GetDashboardTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    private readonly RecordingDashboardReader _reader = new();

    [Fact]
    public async Task A_qa_manager_gets_the_numbers_for_now_and_the_last_ninety_days()
    {
        var result = await GetAsync(Role.QaManager);

        Assert.Same(_reader.View, result.Value);
        Assert.Equal(new DashboardPeriod(_now, ClosedSince: _now.AddDays(-90)), _reader.AskedFor);
    }

    [Theory]
    [InlineData(Role.Inspector)]
    [InlineData(Role.Investigator)]
    public async Task Only_a_qa_manager_may_see_the_dashboard(Role role)
    {
        var result = await GetAsync(role);

        Assert.Equal(ErrorKind.Forbidden, result.Error.Kind);
        Assert.Null(_reader.AskedFor);
    }

    private Task<Result<DashboardView>> GetAsync(Role role) =>
        new GetDashboardHandler(_reader, new FixedClock(_now), new FakeCurrentUser(role)).HandleAsync(CancellationToken.None);

    private sealed class RecordingDashboardReader : IDashboardReader
    {
        public DashboardView View { get; } = new([new SeverityCount(Severity.Major, 3)], OverdueCount: 1, AverageDaysToClose: 4.5);

        public DashboardPeriod? AskedFor { get; private set; }

        public Task<DashboardView> ReadAsync(DashboardPeriod period, CancellationToken cancellationToken)
        {
            AskedFor = period;
            return Task.FromResult(View);
        }
    }
}
