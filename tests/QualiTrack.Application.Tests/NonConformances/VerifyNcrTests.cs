using QualiTrack.Application.Abstractions;
using QualiTrack.Application.NonConformances;
using QualiTrack.Application.Tests.Fakes;
using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Tests.NonConformances;

public sealed class VerifyNcrTests
{
    private static readonly DateTimeOffset _now = Ncrs.RaisedAt.AddDays(5);

    private readonly InMemoryNcrRepository _repository = new();
    private readonly CountingUnitOfWork _unitOfWork = new();
    private readonly UserId _investigator = new(Guid.NewGuid());
    private readonly FakeCurrentUser _manager = new(Role.QaManager);

    [Fact]
    public async Task A_qa_manager_verifying_the_fix_closes_the_ncr()
    {
        var ncr = Stored(Ncrs.PendingVerification(_investigator));

        var result = await CreateHandler(_manager).HandleAsync(new(ncr.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NcrStatus.Closed, ncr.Status);
        Assert.Equal(new AuditStamp(_manager.Id, _now), ncr.History[^1].Stamp);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(Role.Inspector)]
    [InlineData(Role.Investigator)]
    public async Task Only_a_qa_manager_can_verify(Role role)
    {
        var ncr = Stored(Ncrs.PendingVerification(_investigator));

        var result = await CreateHandler(new FakeCurrentUser(role)).HandleAsync(new(ncr.Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Forbidden, result);
    }

    [Fact]
    public async Task Verifying_an_unknown_ncr_is_not_found()
    {
        var result = await CreateHandler(_manager).HandleAsync(new(Guid.NewGuid()), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.NotFound, result);
    }

    [Fact]
    public async Task Verifying_before_the_fix_is_submitted_is_a_conflict()
    {
        var ncr = Stored(Ncrs.ActionInProgress(_investigator));

        var result = await CreateHandler(_manager).HandleAsync(new(ncr.Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Conflict, result);
    }

    private NonConformance Stored(NonConformance ncr)
    {
        _repository.Add(ncr);
        return ncr;
    }

    private VerifyNcrHandler CreateHandler(ICurrentUser currentUser) =>
        new(new NcrCommandRunner(_repository, _unitOfWork, new FixedClock(_now), currentUser), currentUser);

    private void AssertFailedWithoutSaving(ErrorKind expected, Result result)
    {
        Assert.Equal(expected, result.Error.Kind);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}
