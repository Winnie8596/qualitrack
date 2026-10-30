using Microsoft.EntityFrameworkCore;
using QualiTrack.Application.Dashboard;
using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;
using QualiTrack.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace QualiTrack.Api.IntegrationTests.Persistence;

/// <summary>
/// The dashboard's numbers against real PostgreSQL. It counts every NCR, so it has a database of its own: the
/// shared one holds whatever other tests saved.
/// </summary>
public sealed class DashboardReaderTests : IAsyncLifetime
{
    private static readonly DateTimeOffset _now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId _inspector = new(Guid.NewGuid());
    private static readonly UserId _investigator = new(Guid.NewGuid());
    private static readonly UserId _manager = new(Guid.NewGuid());

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private int _lastSequence;

    [Fact]
    public async Task An_empty_database_has_zero_of_each_severity_and_no_average()
    {
        var view = await ReadAsync();

        Assert.Equal(
            [new SeverityCount(Severity.Minor, 0), new SeverityCount(Severity.Major, 0), new SeverityCount(Severity.Critical, 0)],
            view.UnfinishedBySeverity);
        Assert.Equal(0, view.OverdueCount);
        Assert.Null(view.AverageDaysToClose);
    }

    [Fact]
    public async Task The_numbers_count_unfinished_ncrs_by_severity_the_overdue_ones_and_recent_closing_times()
    {
        await SaveAsync(
            Investigating(Severity.Major, raisedDaysAgo: 10, dueInDaysAfterRaised: 5),
            Investigating(Severity.Major, raisedDaysAgo: 2, dueInDaysAfterRaised: 5),
            Raised(Severity.Critical, raisedDaysAgo: 1),
            Cancelled(Severity.Major, raisedDaysAgo: 20),
            Closed(Severity.Minor, raisedDaysAgo: 30, daysToClose: 3),
            Closed(Severity.Minor, raisedDaysAgo: 40, daysToClose: 5),
            Closed(Severity.Major, raisedDaysAgo: 300, daysToClose: 20));

        var view = await ReadAsync();

        Assert.Equal(
            [new SeverityCount(Severity.Minor, 0), new SeverityCount(Severity.Major, 2), new SeverityCount(Severity.Critical, 1)],
            view.UnfinishedBySeverity);
        Assert.Equal(1, view.OverdueCount);
        // Closed 27 and 35 days ago, inside the 90 days; the one closed 280 days ago is left out
        Assert.Equal(4.0, view.AverageDaysToClose);
    }

    public Task InitializeAsync() => InitializeDatabaseAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private async Task InitializeDatabaseAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    private QualiTrackDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<QualiTrackDbContext>().UseNpgsql(_container.GetConnectionString()).Options);

    private async Task<DashboardView> ReadAsync()
    {
        await using var context = CreateContext();
        return await new DatabaseDashboardReader(context)
            .ReadAsync(new DashboardPeriod(_now, ClosedSince: _now.AddDays(-90)), CancellationToken.None);
    }

    private async Task SaveAsync(params NonConformance[] ncrs)
    {
        await using var context = CreateContext();
        context.NonConformances.AddRange(ncrs);
        await context.SaveChangesAsync();
    }

    private NonConformance Raised(Severity severity, int raisedDaysAgo) =>
        NonConformance.Raise(
            NcrNumber.From(2026, ++_lastSequence),
            new NcrDetails("Burr on flange", "Sharp burr on the mating face.", "Line 3", severity),
            new AuditStamp(_inspector, _now.AddDays(-raisedDaysAgo)));

    private NonConformance Investigating(Severity severity, int raisedDaysAgo, int dueInDaysAfterRaised)
    {
        var ncr = Raised(severity, raisedDaysAgo);
        ncr.AssignInvestigator(_investigator, ncr.Raised.At.AddDays(dueInDaysAfterRaised), At(ncr, hours: 1, _manager));
        return ncr;
    }

    private NonConformance Cancelled(Severity severity, int raisedDaysAgo)
    {
        var ncr = Raised(severity, raisedDaysAgo);
        ncr.Cancel("Raised by mistake", At(ncr, hours: 1, _manager));
        return ncr;
    }

    private NonConformance Closed(Severity severity, int raisedDaysAgo, int daysToClose)
    {
        var ncr = Investigating(severity, raisedDaysAgo, dueInDaysAfterRaised: 7);
        ncr.PlanFix("Deburring step skipped", ["Deburr the flange"], At(ncr, hours: 2, _investigator));
        ncr.CompleteAction(ncr.Actions[0].Id, At(ncr, hours: 3, _investigator));
        ncr.SubmitForVerification(At(ncr, hours: 3, _investigator));
        ncr.Verify(new AuditStamp(_manager, ncr.Raised.At.AddDays(daysToClose)));
        return ncr;
    }

    private static AuditStamp At(NonConformance ncr, int hours, UserId by) => new(by, ncr.Raised.At.AddHours(hours));
}
