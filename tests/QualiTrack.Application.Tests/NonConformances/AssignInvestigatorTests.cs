using QualiTrack.Application.Abstractions;
using QualiTrack.Application.NonConformances;
using QualiTrack.Application.Tests.Fakes;
using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Tests.NonConformances;

public sealed class AssignInvestigatorTests
{
    private static readonly DateTimeOffset _now = Ncrs.RaisedAt.AddDays(1);
    private static readonly DateTimeOffset _validDueDate = Ncrs.RaisedAt.AddDays(7);

    private readonly InMemoryNcrRepository _repository = new();
    private readonly CountingUnitOfWork _unitOfWork = new();
    private readonly InMemoryUserDirectory _users = new();
    private readonly UserId _investigator;

    public AssignInvestigatorTests() => _investigator = _users.Add(Role.Investigator);

    [Fact]
    public async Task A_qa_manager_assigning_an_investigator_starts_the_investigation()
    {
        var ncr = Stored(Ncrs.Open());
        var manager = new FakeCurrentUser(Role.QaManager);

        var result = await CreateHandler(manager).HandleAsync(Command(ncr.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NcrStatus.Investigating, ncr.Status);
        Assert.Equal(_investigator, ncr.InvestigatorId);
        Assert.Equal(_validDueDate, ncr.DueDate);
        var change = Assert.Single(ncr.History);
        Assert.Equal(new AuditStamp(manager.Id, _now), change.Stamp);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(Role.Inspector)]
    [InlineData(Role.Investigator)]
    public async Task Only_a_qa_manager_can_assign_an_investigator(Role role)
    {
        var ncr = Stored(Ncrs.Open());

        var result = await CreateHandler(new FakeCurrentUser(role)).HandleAsync(Command(ncr.Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Forbidden, result);
    }

    [Fact]
    public async Task Assigning_on_an_unknown_ncr_is_not_found()
    {
        var result = await CreateHandler().HandleAsync(Command(Guid.NewGuid()), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.NotFound, result);
    }

    [Theory]
    [InlineData(Role.Inspector)]
    [InlineData(Role.QaManager)]
    public async Task The_assignee_must_have_the_investigator_role(Role role)
    {
        var ncr = Stored(Ncrs.Open());
        var command = Command(ncr.Id) with { InvestigatorId = _users.Add(role).Value };

        var result = await CreateHandler().HandleAsync(command, CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Validation, result);
    }

    [Fact]
    public async Task An_assignee_who_is_not_a_known_user_is_a_validation_error()
    {
        var ncr = Stored(Ncrs.Open());
        var command = Command(ncr.Id) with { InvestigatorId = Guid.NewGuid() };

        var result = await CreateHandler().HandleAsync(command, CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Validation, result);
    }

    [Fact]
    public async Task A_critical_ncr_due_more_than_7_days_after_it_was_raised_is_a_validation_error()
    {
        var ncr = Stored(Ncrs.Open(Severity.Critical));
        var command = Command(ncr.Id) with { DueDate = Ncrs.RaisedAt.AddDays(8) };

        var result = await CreateHandler().HandleAsync(command, CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Validation, result);
        Assert.Equal(NcrStatus.Open, ncr.Status);
    }

    [Fact]
    public async Task Assigning_a_cancelled_ncr_is_a_conflict()
    {
        var ncr = Stored(Ncrs.Cancelled());

        var result = await CreateHandler().HandleAsync(Command(ncr.Id), CancellationToken.None);

        AssertFailedWithoutSaving(ErrorKind.Conflict, result);
    }

    private AssignInvestigatorCommand Command(Guid ncrId) => new(ncrId, _investigator.Value, _validDueDate);

    private NonConformance Stored(NonConformance ncr)
    {
        _repository.Add(ncr);
        return ncr;
    }

    private AssignInvestigatorHandler CreateHandler(ICurrentUser? currentUser = null)
    {
        var caller = currentUser ?? new FakeCurrentUser(Role.QaManager);
        return new(new NcrCommandRunner(_repository, _unitOfWork, new FixedClock(_now), caller), caller, _users);
    }

    private void AssertFailedWithoutSaving(ErrorKind expected, Result result)
    {
        Assert.Equal(expected, result.Error.Kind);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}
