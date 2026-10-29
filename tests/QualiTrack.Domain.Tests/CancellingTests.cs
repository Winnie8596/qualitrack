using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Domain.Tests;

public sealed class CancellingTests
{
    [Fact]
    public void Cancelling_an_open_ncr_records_the_reason()
    {
        var ncr = Ncrs.Open();
        var cancelled = Ncrs.Day(1, Ncrs.Manager);

        ncr.Cancel("Raised twice by mistake", cancelled);

        Assert.Equal(NcrStatus.Cancelled, ncr.Status);
        Assert.Equal(new StatusChange(NcrStatus.Open, NcrStatus.Cancelled, cancelled, "Raised twice by mistake"), ncr.History[^1]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancelling_requires_a_reason(string reason)
    {
        var ncr = Ncrs.Open();

        var exception = Record.Exception(() => ncr.Cancel(reason, Ncrs.Day(1, Ncrs.Manager)));

        Assert.IsType<DomainException>(exception);
    }

    [Fact]
    public void Only_an_open_ncr_can_be_cancelled()
    {
        var ncr = Ncrs.Investigating();

        var exception = Record.Exception(() => ncr.Cancel("No longer needed", Ncrs.Day(2, Ncrs.Manager)));

        Assert.IsType<DomainException>(exception);
    }
}
