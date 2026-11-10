using QualiTrack.Application.Abstractions;
using QualiTrack.Application.NonConformances;
using QualiTrack.Application.Tests.Fakes;
using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Tests.NonConformances;

public sealed class CompleteCorrectiveActionTests
{
    private static readonly DateTimeOffset _now = Ncrs.RaisedAt.AddDays(3);

    private readonly InMemoryNcrRepository _repository = new();
    private readonly CountingUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUser _investigator = new(Role.Investigator);

    [Fact]
    public async Task The_assigned_investigator_completing_an_action_stores_who_and_when()
    {
        var ncr = Stored(Ncrs.ActionInProgress(_investigator.Id));
        var action = ncr.Actions[0];

        var result = await CreateHandler(_investigator).HandleAsync(new(ncr.Id, action.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new AuditStamp(_investigator.Id, _now), action.Completed);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(Role.Inspector)]
    [InlineData(Role.QaManager)]
    public async Task Only_an_investigator_can_complete_an_action(Role role)
    {
        var ncr = Stored(Ncrs.ActionInProgress(_investigator.Id));

        var result = await CreateHandler(new FakeCurrentUser(role)).HandleAsync(new(ncr.Id, ncr.Actions[0].Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Forbidden, result);
    }

    [Fact]
    public async Task Completing_an_action_on_an_unknown_ncr_is_not_found()
    {
        var result = await CreateHandler(_investigator).HandleAsync(new(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.NotFound, result);
    }

    [Fact]
    public async Task An_investigator_who_is_not_assigned_cannot_complete_an_action()
    {
        var ncr = Stored(Ncrs.ActionInProgress(_investigator.Id));
        var otherInvestigator = new FakeCurrentUser(Role.Investigator);

        var result = await CreateHandler(otherInvestigator).HandleAsync(new(ncr.Id, ncr.Actions[0].Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Forbidden, result);
    }

    [Fact]
    public async Task Completing_an_action_that_is_not_on_this_ncr_is_a_validation_error()
    {
        var ncr = Stored(Ncrs.ActionInProgress(_investigator.Id));

        var result = await CreateHandler(_investigator).HandleAsync(new(ncr.Id, Guid.NewGuid()), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Validation, result);
    }

    [Fact]
    public async Task Completing_an_action_twice_is_a_conflict()
    {
        var ncr = Stored(Ncrs.ActionInProgress(_investigator.Id));
        var action = ncr.Actions[0];
        ncr.CompleteAction(action.Id, new AuditStamp(_investigator.Id, _now));

        var result = await CreateHandler(_investigator).HandleAsync(new(ncr.Id, action.Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Conflict, result);
    }

    private NonConformance Stored(NonConformance ncr)
    {
        _repository.Add(ncr);
        return ncr;
    }

    private CompleteCorrectiveActionHandler CreateHandler(ICurrentUser currentUser) =>
        new(new NcrCommandRunner(_repository, _unitOfWork, new FixedClock(_now), currentUser), currentUser);

    private void AssertFailedWithoutSaving(ErrorKind expected, Result result)
    {
        Assert.Equal(expected, result.Error.Kind);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}
