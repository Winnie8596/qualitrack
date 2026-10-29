using QualiTrack.Application.Abstractions;
using QualiTrack.Application.NonConformances;
using QualiTrack.Application.Tests.Fakes;
using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Tests.NonConformances;

public sealed class SubmitForVerificationTests
{
    private static readonly DateTimeOffset _now = Ncrs.RaisedAt.AddDays(4);

    private readonly InMemoryNcrRepository _repository = new();
    private readonly CountingUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUser _investigator = new(Role.Investigator);

    [Fact]
    public async Task Submitting_with_every_action_completed_awaits_verification()
    {
        var ncr = Stored(ActionsCompleted());

        var result = await CreateHandler(_investigator).HandleAsync(new(ncr.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NcrStatus.PendingVerification, ncr.Status);
        Assert.Equal(new AuditStamp(_investigator.Id, _now), ncr.History[^1].Stamp);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(Role.Inspector)]
    [InlineData(Role.QaManager)]
    public async Task Only_an_investigator_can_submit_for_verification(Role role)
    {
        var ncr = Stored(ActionsCompleted());

        var result = await CreateHandler(new FakeCurrentUser(role)).HandleAsync(new(ncr.Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Forbidden, result);
    }

    [Fact]
    public async Task Submitting_an_unknown_ncr_is_not_found()
    {
        var result = await CreateHandler(_investigator).HandleAsync(new(Guid.NewGuid()), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.NotFound, result);
    }

    [Fact]
    public async Task An_investigator_who_is_not_assigned_cannot_submit()
    {
        var ncr = Stored(ActionsCompleted());
        var otherInvestigator = new FakeCurrentUser(Role.Investigator);

        var result = await CreateHandler(otherInvestigator).HandleAsync(new(ncr.Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Forbidden, result);
    }

    [Fact]
    public async Task Submitting_while_an_action_is_incomplete_is_a_conflict()
    {
        var ncr = Stored(Ncrs.ActionInProgress(_investigator.Id));

        var result = await CreateHandler(_investigator).HandleAsync(new(ncr.Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Conflict, result);
        Assert.Equal(NcrStatus.ActionInProgress, ncr.Status);
    }

    private NonConformance ActionsCompleted()
    {
        var ncr = Ncrs.ActionInProgress(_investigator.Id);
        ncr.CompleteAction(ncr.Actions[0].Id, new AuditStamp(_investigator.Id, _now.AddHours(-1)));
        return ncr;
    }

    private NonConformance Stored(NonConformance ncr)
    {
        _repository.Add(ncr);
        return ncr;
    }

    private SubmitForVerificationHandler CreateHandler(ICurrentUser currentUser) =>
        new(new NcrCommandRunner(_repository, _unitOfWork, new FixedClock(_now), currentUser), currentUser);

    private void AssertFailedWithoutSaving(ErrorKind expected, Result result)
    {
        Assert.Equal(expected, result.Error.Kind);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}
