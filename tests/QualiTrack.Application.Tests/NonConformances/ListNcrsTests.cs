using QualiTrack.Application.Abstractions;
using QualiTrack.Application.NonConformances;
using QualiTrack.Application.Tests.Fakes;
using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Tests.NonConformances;

public sealed class ListNcrsTests
{
    private static readonly DateTimeOffset _now = Ncrs.RaisedAt.AddDays(1);

    private readonly InMemoryNcrRepository _repository = new();
    private readonly FakeCurrentUser _manager = new(Role.QaManager);
    private readonly UserId _investigator = new(Guid.NewGuid());

    [Fact]
    public async Task An_inspector_sees_only_the_ncrs_they_raised()
    {
        var own = Stored(Ncrs.Open());
        Stored(RaisedBySomeoneElse());

        var page = await ListAsync(new FakeCurrentUser(Ncrs.Inspector, Role.Inspector));

        Assert.Equal([own.Id], page.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task An_investigator_sees_the_ncrs_they_raised_or_are_assigned_to()
    {
        var assigned = Stored(Ncrs.Investigating(_investigator));
        var raised = Stored(RaisedBy(_investigator));
        Stored(Ncrs.Investigating(new UserId(Guid.NewGuid())));

        var page = await ListAsync(new FakeCurrentUser(_investigator, Role.Investigator));

        Assert.Equivalent(new[] { assigned.Id, raised.Id }, page.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task A_qa_manager_sees_every_ncr()
    {
        Stored(Ncrs.Open());
        Stored(RaisedBySomeoneElse());

        var page = await ListAsync(_manager);

        Assert.Equal(2, page.Items.Count);
    }

    [Fact]
    public async Task The_list_can_be_filtered_by_status()
    {
        var investigating = Stored(Ncrs.Investigating(_investigator));
        Stored(Ncrs.Open());

        var page = await ListAsync(_manager, Query() with { Status = NcrStatus.Investigating });

        Assert.Equal([investigating.Id], page.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task The_list_can_be_filtered_by_severity()
    {
        var critical = Stored(Ncrs.Open(Severity.Critical));
        Stored(Ncrs.Open(Severity.Minor));

        var page = await ListAsync(_manager, Query() with { Severity = Severity.Critical });

        Assert.Equal([critical.Id], page.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task The_list_can_be_filtered_to_overdue_ncrs()
    {
        var overdue = Stored(Ncrs.Investigating(_investigator));
        Stored(Ncrs.Open());

        var page = await ListAsync(_manager, Query() with { OverdueOnly = true }, now: Ncrs.RaisedAt.AddDays(8));

        Assert.Equal([overdue.Id], page.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task Each_item_shows_the_summary_the_list_page_needs()
    {
        var ncr = Stored(Ncrs.Investigating(_investigator));

        var item = Assert.Single((await ListAsync(_manager, now: Ncrs.RaisedAt.AddDays(8))).Items);

        var expected = new NcrSummaryView(
            ncr.Id,
            "NCR-2026-0001",
            "Weld crack on beam B4",
            Severity.Major,
            NcrStatus.Investigating,
            Ncrs.RaisedAt,
            Ncrs.RaisedAt.AddDays(7),
            IsOverdue: true);
        Assert.Equal(expected, item);
    }

    [Fact]
    public async Task A_page_holds_twenty_ncrs_and_says_whether_another_follows()
    {
        for (var i = 0; i < ListNcrsHandler.PageSize + 1; i++)
        {
            Stored(Ncrs.Open());
        }

        var first = await ListAsync(_manager);
        var second = await ListAsync(_manager, Query() with { Page = 2 });

        Assert.Equal(20, first.Items.Count);
        Assert.True(first.HasNextPage);
        Assert.Single(second.Items);
        Assert.False(second.HasNextPage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task A_page_number_below_one_is_rejected(int page)
    {
        var result = await HandleAsync(_manager, Query() with { Page = page }, _now);

        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
    }

    private static ListNcrsQuery Query() => new();

    private static NonConformance RaisedBySomeoneElse() => RaisedBy(new UserId(Guid.NewGuid()));

    private static NonConformance RaisedBy(UserId user) =>
        NonConformance.Raise(
            NcrNumber.From(2026, 2),
            new NcrDetails("Paint run", "Runs on the primer coat.", "Bay 1", Severity.Minor),
            new AuditStamp(user, Ncrs.RaisedAt));

    private NonConformance Stored(NonConformance ncr)
    {
        _repository.Add(ncr);
        return ncr;
    }

    private async Task<NcrListPage> ListAsync(ICurrentUser currentUser, ListNcrsQuery? query = null, DateTimeOffset? now = null) =>
        (await HandleAsync(currentUser, query ?? Query(), now ?? _now)).Value;

    private Task<Result<NcrListPage>> HandleAsync(ICurrentUser currentUser, ListNcrsQuery query, DateTimeOffset now) =>
        new ListNcrsHandler(_repository, new FixedClock(now), currentUser).HandleAsync(query, CancellationToken.None);
}
