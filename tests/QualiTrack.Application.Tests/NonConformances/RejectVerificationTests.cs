using QualiTrack.Application.Abstractions;
using QualiTrack.Application.NonConformances;
using QualiTrack.Application.Tests.Fakes;
using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Tests.NonConformances;

public sealed class RejectVerificationTests
{
    private static readonly DateTimeOffset _now = Ncrs.RaisedAt.AddDays(5);

    private readonly InMemoryNcrRepository _repository = new();
    private readonly CountingUnitOfWork _unitOfWork = new();
    private readonly UserId _investigator = new(Guid.NewGuid());
    private readonly FakeCurrentUser _manager = new(Role.QaManager);

    [Fact]
    public async Task A_qa_manager_rejecting_the_fix_sends_it_back_with_the_reason()
    {
        var ncr = Stored(Ncrs.PendingVerification(_investigator));

        var result = await CreateHandler(_manager).HandleAsync(Command(ncr.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NcrStatus.ActionInProgress, ncr.Status);
        var change = ncr.History[^1];
        Assert.Equal("Weld still shows porosity", change.Note);
        Assert.Equal(new AuditStamp(_manager.Id, _now), change.Stamp);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(Role.Inspector)]
    [InlineData(Role.Investigator)]
    public async Task Only_a_qa_manager_can_reject(Role role)
    {
        var ncr = Stored(Ncrs.PendingVerification(_investigator));

        var result = await CreateHandler(new FakeCurrentUser(role)).HandleAsync(Command(ncr.Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Forbidden, result);
    }

    [Fact]
    public async Task Rejecting_an_unknown_ncr_is_not_found()
    {
        var result = await CreateHandler(_manager).HandleAsync(Command(Guid.NewGuid()), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.NotFound, result);
    }

    [Fact]
    public async Task Rejecting_without_a_reason_is_a_validation_error()
    {
        var ncr = Stored(Ncrs.PendingVerification(_investigator));
        var command = Command(ncr.Id) with { Reason = " " };

        var result = await CreateHandler(_manager).HandleAsync(command, CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Validation, result);
    }

    [Fact]
    public async Task Rejecting_before_the_fix_is_submitted_is_a_conflict()
    {
        var ncr = Stored(Ncrs.ActionInProgress(_investigator));

        var result = await CreateHandler(_manager).HandleAsync(Command(ncr.Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Conflict, result);
    }

    private static RejectVerificationCommand Command(Guid ncrId) => new(ncrId, "Weld still shows porosity");

    private NonConformance Stored(NonConformance ncr)
    {
        _repository.Add(ncr);
        return ncr;
    }

    private RejectVerificationHandler CreateHandler(ICurrentUser currentUser) =>
        new(new NcrCommandRunner(_repository, _unitOfWork, new FixedClock(_now), currentUser), currentUser);

    private void AssertFailedWithoutSaving(ErrorKind expected, Result result)
    {
        Assert.Equal(expected, result.Error.Kind);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}
