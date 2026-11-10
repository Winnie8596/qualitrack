using QualiTrack.Application.Abstractions;
using QualiTrack.Application.NonConformances;
using QualiTrack.Application.Tests.Fakes;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Tests.NonConformances;

public sealed class RaiseNcrTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 29, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryNcrRepository _repository = new();
    private readonly CountingUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task An_inspector_raises_an_open_ncr_numbered_in_this_years_sequence()
    {
        var inspector = new FakeCurrentUser(Role.Inspector);
        var handler = CreateHandler(inspector);

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var ncr = Assert.Single(_repository.All);
        Assert.Equal(result.Value, ncr.Id);
        Assert.Equal(NcrStatus.Open, ncr.Status);
        Assert.Equal("NCR-2026-0001", ncr.Number.Value);
        Assert.Equal(inspector.Id, ncr.Raised.By);
        Assert.Equal(_now, ncr.Raised.At);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(Role.Investigator)]
    [InlineData(Role.QaManager)]
    public async Task Only_an_inspector_can_raise_an_ncr(Role role)
    {
        var handler = CreateHandler(new FakeCurrentUser(role));

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.Equal(ErrorKind.Forbidden, result.Error.Kind);
        AssertNothingSaved();
    }

    [Fact]
    public async Task A_blank_title_is_a_validation_error_and_nothing_is_saved()
    {
        var handler = CreateHandler(new FakeCurrentUser(Role.Inspector));
        var command = ValidCommand() with { Title = "   " };

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        AssertNothingSaved();
    }

    private static RaiseNcrCommand ValidCommand() =>
        new("Weld crack on beam B4", "Hairline crack along the fillet weld.", "Bay 3", Severity.Major);

    private RaiseNcrHandler CreateHandler(ICurrentUser currentUser) =>
        new(_repository, _unitOfWork, new FixedClock(_now), currentUser, new SequentialNcrNumberGenerator());

    private void AssertNothingSaved()
    {
        Assert.Empty(_repository.All);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}
