using QualiTrack.Application.Abstractions;
using QualiTrack.Application.NonConformances;
using QualiTrack.Application.Tests.Fakes;
using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Tests.NonConformances;

public sealed class AddCorrectiveActionTests
{
    private static readonly DateTimeOffset _now = Ncrs.RaisedAt.AddDays(2);

    private readonly InMemoryNcrRepository _repository = new();
    private readonly CountingUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUser _investigator = new(Role.Investigator);

    [Fact]
    public async Task The_assigned_investigator_adds_an_action_to_the_fix()
    {
        var ncr = Stored(Ncrs.ActionInProgress(_investigator.Id));

        var result = await CreateHandler(_investigator).HandleAsync(Command(ncr.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var added = ncr.Actions[^1];
        Assert.Equal("Inspect the other welds on beam B4", added.Description);
        Assert.Equal(new AuditStamp(_investigator.Id, _now), added.Added);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(Role.Inspector)]
    [InlineData(Role.QaManager)]
    public async Task Only_an_investigator_can_add_an_action(Role role)
    {
        var ncr = Stored(Ncrs.ActionInProgress(_investigator.Id));

        var result = await CreateHandler(new FakeCurrentUser(role)).HandleAsync(Command(ncr.Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Forbidden, result);
    }

    [Fact]
    public async Task Adding_an_action_to_an_unknown_ncr_is_not_found()
    {
        var result = await CreateHandler(_investigator).HandleAsync(Command(Guid.NewGuid()), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.NotFound, result);
    }

    [Fact]
    public async Task An_investigator_who_is_not_assigned_cannot_add_an_action()
    {
        var ncr = Stored(Ncrs.ActionInProgress(_investigator.Id));

        var result = await CreateHandler(new FakeCurrentUser(Role.Investigator)).HandleAsync(Command(ncr.Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Forbidden, result);
    }

    [Fact]
    public async Task A_blank_action_description_is_a_validation_error()
    {
        var ncr = Stored(Ncrs.ActionInProgress(_investigator.Id));
        var command = Command(ncr.Id) with { Description = "" };

        var result = await CreateHandler(_investigator).HandleAsync(command, CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Validation, result);
    }

    [Fact]
    public async Task Adding_an_action_before_the_fix_is_planned_is_a_conflict()
    {
        var ncr = Stored(Ncrs.Investigating(_investigator.Id));

        var result = await CreateHandler(_investigator).HandleAsync(Command(ncr.Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Conflict, result);
    }

    private static AddCorrectiveActionCommand Command(Guid ncrId) => new(ncrId, "Inspect the other welds on beam B4");

    private NonConformance Stored(NonConformance ncr)
    {
        _repository.Add(ncr);
        return ncr;
    }

    private AddCorrectiveActionHandler CreateHandler(ICurrentUser currentUser) =>
        new(new NcrCommandRunner(_repository, _unitOfWork, new FixedClock(_now), currentUser), currentUser);

    private void AssertFailedWithoutSaving(ErrorKind expected, Result result)
    {
        Assert.Equal(expected, result.Error.Kind);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}
