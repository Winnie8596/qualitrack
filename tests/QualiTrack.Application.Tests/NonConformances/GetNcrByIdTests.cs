using QualiTrack.Application.Abstractions;
using QualiTrack.Application.NonConformances;
using QualiTrack.Application.Tests.Fakes;
using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Tests.NonConformances;

public sealed class GetNcrByIdTests
{
    private static readonly DateTimeOffset _now = Ncrs.RaisedAt.AddHours(4);

    private readonly InMemoryNcrRepository _repository = new();
    private readonly FakeCurrentUser _manager = new(Role.QaManager);
    private readonly UserId _investigator = new(Guid.NewGuid());

    [Fact]
    public async Task The_view_shows_the_details_investigator_due_date_root_cause_actions_and_history()
    {
        var ncr = Stored(Ncrs.ActionInProgress(_investigator));

        var view = (await GetAsync(ncr.Id, _manager)).Value;

        var action = ncr.Actions[0];
        var expected = new NcrView(
            ncr.Id,
            "NCR-2026-0001",
            "Weld crack on beam B4",
            "Hairline crack along the fillet weld.",
            "Bay 3",
            Severity.Major,
            NcrStatus.ActionInProgress,
            Ncrs.Inspector.Value,
            Ncrs.RaisedAt,
            _investigator.Value,
            Ncrs.RaisedAt.AddDays(7),
            IsOverdue: false,
            "Wrong electrode for the steel grade",
            [new CorrectiveActionView(action.Id, "Grind out the weld", _investigator.Value, Ncrs.RaisedAt.AddHours(2), null, null)],
            [.. ncr.History.Select(change => new StatusChangeView(change.From, change.To, change.Stamp.By.Value, change.Stamp.At, change.Note))]);
        Assert.Equal(expected with { Actions = [], History = [] }, view with { Actions = [], History = [] });
        Assert.Equal(expected.Actions, view.Actions);
        Assert.Equal(expected.History, view.History);
        Assert.Equal([NcrStatus.Investigating, NcrStatus.ActionInProgress], view.History.Select(change => change.To));
    }

    [Fact]
    public async Task A_completed_action_shows_who_completed_it_and_when()
    {
        var ncr = Stored(Ncrs.PendingVerification(_investigator));

        var action = Assert.Single((await GetAsync(ncr.Id, _manager)).Value.Actions);

        Assert.Equal(_investigator.Value, action.CompletedBy);
        Assert.Equal(Ncrs.RaisedAt.AddHours(3), action.CompletedAt);
    }

    [Fact]
    public async Task An_ncr_past_its_due_date_is_shown_as_overdue()
    {
        var ncr = Stored(Ncrs.Investigating(_investigator));

        var view = (await GetAsync(ncr.Id, _manager, now: Ncrs.RaisedAt.AddDays(8))).Value;

        Assert.True(view.IsOverdue);
    }

    [Fact]
    public async Task An_inspector_sees_an_ncr_they_raised()
    {
        var ncr = Stored(Ncrs.Open());

        var result = await GetAsync(ncr.Id, new FakeCurrentUser(Ncrs.Inspector, Role.Inspector));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task An_investigator_sees_an_ncr_assigned_to_them()
    {
        var ncr = Stored(Ncrs.Investigating(_investigator));

        var result = await GetAsync(ncr.Id, new FakeCurrentUser(_investigator, Role.Investigator));

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(Role.Inspector)]
    [InlineData(Role.Investigator)]
    public async Task An_ncr_the_caller_may_not_see_is_not_found_so_its_existence_is_not_revealed(Role role)
    {
        var ncr = Stored(Ncrs.Investigating(_investigator));

        var result = await GetAsync(ncr.Id, new FakeCurrentUser(role));

        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
    }

    [Fact]
    public async Task An_unknown_ncr_is_not_found()
    {
        var result = await GetAsync(Guid.NewGuid(), _manager);

        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
    }

    private NonConformance Stored(NonConformance ncr)
    {
        _repository.Add(ncr);
        return ncr;
    }

    private Task<Result<NcrView>> GetAsync(Guid id, ICurrentUser currentUser, DateTimeOffset? now = null) =>
        new GetNcrByIdHandler(_repository, new FixedClock(now ?? _now), currentUser)
            .HandleAsync(new GetNcrByIdQuery(id), CancellationToken.None);
}
