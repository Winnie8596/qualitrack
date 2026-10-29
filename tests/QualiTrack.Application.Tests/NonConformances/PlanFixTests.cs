using QualiTrack.Application.Abstractions;
using QualiTrack.Application.NonConformances;
using QualiTrack.Application.Tests.Fakes;
using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Tests.NonConformances;

public sealed class PlanFixTests
{
    private static readonly DateTimeOffset _now = Ncrs.RaisedAt.AddDays(2);

    private readonly InMemoryNcrRepository _repository = new();
    private readonly CountingUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUser _investigator = new(Role.Investigator);

    [Fact]
    public async Task The_assigned_investigator_planning_the_fix_starts_the_corrective_work()
    {
        var ncr = Stored(Ncrs.Investigating(_investigator.Id));

        var result = await CreateHandler(_investigator).HandleAsync(Command(ncr.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NcrStatus.ActionInProgress, ncr.Status);
        Assert.Equal("Wrong electrode for the steel grade", ncr.RootCause);
        Assert.Equal(["Grind out the weld", "Re-weld with E7018"], ncr.Actions.Select(action => action.Description));
        Assert.All(ncr.Actions, action => Assert.Equal(new AuditStamp(_investigator.Id, _now), action.Added));
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(Role.Inspector)]
    [InlineData(Role.QaManager)]
    public async Task Only_an_investigator_can_plan_a_fix(Role role)
    {
        var ncr = Stored(Ncrs.Investigating(_investigator.Id));

        var result = await CreateHandler(new FakeCurrentUser(role)).HandleAsync(Command(ncr.Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Forbidden, result);
    }

    [Fact]
    public async Task Planning_the_fix_on_an_unknown_ncr_is_not_found()
    {
        var result = await CreateHandler(_investigator).HandleAsync(Command(Guid.NewGuid()), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.NotFound, result);
    }

    [Fact]
    public async Task An_investigator_who_is_not_assigned_cannot_plan_the_fix()
    {
        var ncr = Stored(Ncrs.Investigating(_investigator.Id));
        var otherInvestigator = new FakeCurrentUser(Role.Investigator);

        var result = await CreateHandler(otherInvestigator).HandleAsync(Command(ncr.Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Forbidden, result);
    }

    [Fact]
    public async Task A_blank_root_cause_is_a_validation_error()
    {
        var ncr = Stored(Ncrs.Investigating(_investigator.Id));
        var command = Command(ncr.Id) with { RootCause = "  " };

        var result = await CreateHandler(_investigator).HandleAsync(command, CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Validation, result);
    }

    [Fact]
    public async Task Planning_the_fix_before_an_investigator_is_assigned_is_a_conflict()
    {
        var ncr = Stored(Ncrs.Open());

        var result = await CreateHandler(_investigator).HandleAsync(Command(ncr.Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Conflict, result);
    }

    private static PlanFixCommand Command(Guid ncrId) =>
        new(ncrId, "Wrong electrode for the steel grade", ["Grind out the weld", "Re-weld with E7018"]);

    private NonConformance Stored(NonConformance ncr)
    {
        _repository.Add(ncr);
        return ncr;
    }

    private PlanFixHandler CreateHandler(ICurrentUser currentUser) =>
        new(new NcrCommandRunner(_repository, _unitOfWork, new FixedClock(_now), currentUser), currentUser);

    private void AssertFailedWithoutSaving(ErrorKind expected, Result result)
    {
        Assert.Equal(expected, result.Error.Kind);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}
