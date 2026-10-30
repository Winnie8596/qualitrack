using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using QualiTrack.Application.Abstractions;
using QualiTrack.Application.NonConformances;
using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;
using QualiTrack.Infrastructure.Persistence;

namespace QualiTrack.Api.IntegrationTests.Persistence;

/// <summary>
/// The NCR list against real PostgreSQL: every filter must translate to SQL, and the sort order is the
/// database's. Each test lists as its own investigator, so NCRs other tests saved stay out of its results.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class NcrListTests(PostgresFixture database)
{
    private static readonly DateTimeOffset _raisedAt = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly UserId _manager = new(Guid.NewGuid());

    private readonly UserId _investigator = new(Guid.NewGuid());

    [Fact]
    public async Task An_investigator_lists_the_ncrs_they_raised_or_are_assigned_to()
    {
        var raised = Raised(by: _investigator);
        var assigned = Assigned(Raised(), dueInDays: 7);
        var someoneElses = Assigned(Raised(), dueInDays: 7, to: new UserId(Guid.NewGuid()));
        await SaveAsync(raised, assigned, someoneElses);

        var page = await ListAsync(new ListNcrsQuery());

        Assert.Equivalent(new[] { raised.Id, assigned.Id }, page.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task Status_severity_and_overdue_filters_run_in_the_database()
    {
        var match = Assigned(Raised(Severity.Critical, by: _investigator), dueInDays: 2);
        var notOverdue = Assigned(Raised(Severity.Critical, by: _investigator), dueInDays: 7);
        var otherSeverity = Assigned(Raised(Severity.Minor, by: _investigator), dueInDays: 2);
        var otherStatus = Raised(Severity.Critical, by: _investigator);
        await SaveAsync(match, notOverdue, otherSeverity, otherStatus);

        var page = await ListAsync(
            new ListNcrsQuery(NcrStatus.Investigating, Severity.Critical, OverdueOnly: true),
            now: _raisedAt.AddDays(5));

        Assert.Equal([match.Id], page.Items.Select(item => item.Id));
    }

    [Theory]
    [InlineData(SortDirection.Descending, new[] { 2, 1, 0 })]
    [InlineData(SortDirection.Ascending, new[] { 0, 1, 2 })]
    public async Task The_list_sorts_by_raised_date(SortDirection direction, int[] expectedOrder)
    {
        NonConformance[] ncrs = [Raised(by: _investigator, daysAfter: 0), Raised(by: _investigator, daysAfter: 1), Raised(by: _investigator, daysAfter: 2)];
        await SaveAsync(ncrs);

        var page = await ListAsync(new ListNcrsQuery(SortBy: NcrSortField.RaisedAt, Direction: direction));

        Assert.Equal(expectedOrder.Select(index => ncrs[index].Id), page.Items.Select(item => item.Id));
    }

    [Theory]
    [InlineData(SortDirection.Ascending, new[] { 0, 1, 2 })]
    [InlineData(SortDirection.Descending, new[] { 1, 0, 2 })]
    public async Task The_list_sorts_by_due_date_with_ncrs_not_yet_due_last(SortDirection direction, int[] expectedOrder)
    {
        NonConformance[] ncrs = [Assigned(Raised(), dueInDays: 3), Assigned(Raised(), dueInDays: 5), Raised(by: _investigator)];
        await SaveAsync(ncrs);

        var page = await ListAsync(new ListNcrsQuery(SortBy: NcrSortField.DueDate, Direction: direction));

        Assert.Equal(expectedOrder.Select(index => ncrs[index].Id), page.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task A_page_is_fetched_with_one_sql_query()
    {
        await SaveAsync([.. Enumerable.Range(0, ListNcrsHandler.PageSize + 5).Select(_ => Assigned(Raised(), dueInDays: 7))]);
        var counter = new CommandCounter();

        var page = await ListAsync(new ListNcrsQuery(), counter);

        Assert.Equal(ListNcrsHandler.PageSize, page.Items.Count);
        Assert.Equal(1, counter.Count);
    }

    private NonConformance Raised(Severity severity = Severity.Major, UserId? by = null, int daysAfter = 0) =>
        NonConformance.Raise(
            database.NextNumber(),
            new NcrDetails("Weld crack on beam B4", "Hairline crack along the fillet weld.", "Bay 3", severity),
            new AuditStamp(by ?? new UserId(Guid.NewGuid()), _raisedAt.AddDays(daysAfter)));

    private NonConformance Assigned(NonConformance ncr, int dueInDays, UserId? to = null)
    {
        ncr.AssignInvestigator(to ?? _investigator, _raisedAt.AddDays(dueInDays), new AuditStamp(_manager, _raisedAt));
        return ncr;
    }

    private async Task SaveAsync(params NonConformance[] ncrs)
    {
        await using var context = database.CreateContext();
        context.NonConformances.AddRange(ncrs);
        await context.SaveChangesAsync();
    }

    private async Task<NcrListPage> ListAsync(ListNcrsQuery query, CommandCounter? counter = null, DateTimeOffset? now = null)
    {
        await using var context = database.CreateContext(counter is null ? [] : [counter]);
        var handler = new ListNcrsHandler(
            new NcrRepository(context), new Clock(now ?? _raisedAt.AddDays(1)), new Investigator(_investigator));

        var result = await handler.HandleAsync(query, CancellationToken.None);
        return result.Value;
    }

    private sealed class Clock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class Investigator(UserId id) : ICurrentUser
    {
        public UserId Id => id;

        public Role Role => Role.Investigator;
    }

    private sealed class CommandCounter : DbCommandInterceptor
    {
        public int Count { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Count++;
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
