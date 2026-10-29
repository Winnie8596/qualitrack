using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Domain.Tests;

/// <summary>BR-6: a Closed or Cancelled NCR cannot be changed in any way.</summary>
public sealed class FinishedNcrTests
{
    public static TheoryData<string, Action<NonConformance>> Commands => new()
    {
        { "assign", ncr => ncr.AssignInvestigator(Ncrs.Investigator, Ncrs.RaisedAt.AddDays(7), Ncrs.Day(5, Ncrs.Manager)) },
        { "plan fix", ncr => ncr.PlanFix("Root cause", ["Action"], Ncrs.Day(5, Ncrs.Investigator)) },
        { "add action", ncr => ncr.AddCorrectiveAction("Action", Ncrs.Day(5, Ncrs.Investigator)) },
        { "complete action", ncr => ncr.CompleteAction(Guid.NewGuid(), Ncrs.Day(5, Ncrs.Investigator)) },
        { "submit", ncr => ncr.SubmitForVerification(Ncrs.Day(5, Ncrs.Investigator)) },
        { "verify", ncr => ncr.Verify(Ncrs.Day(5, Ncrs.Manager)) },
        { "reject", ncr => ncr.RejectVerification("Reason", Ncrs.Day(5, Ncrs.Manager)) },
        { "cancel", ncr => ncr.Cancel("Reason", Ncrs.Day(5, Ncrs.Manager)) },
    };

    [Fact]
    public void A_closed_ncr_cannot_be_cancelled()
    {
        var ncr = Ncrs.Closed();

        var exception = Record.Exception(() => ncr.Cancel("Too late", Ncrs.Day(5, Ncrs.Manager)));

        Assert.IsType<DomainException>(exception);
    }

    [Fact]
    public void A_cancelled_ncr_cannot_be_assigned()
    {
        var ncr = Ncrs.Cancelled();

        var exception = Record.Exception(() => ncr.AssignInvestigator(Ncrs.Investigator, Ncrs.RaisedAt.AddDays(7), Ncrs.Day(2, Ncrs.Manager)));

        Assert.IsType<DomainException>(exception);
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public void A_closed_ncr_rejects_every_command(string command, Action<NonConformance> act)
    {
        var ncr = Ncrs.Closed();

        var exception = Record.Exception(() => act(ncr));

        Assert.True(exception is DomainException, $"'{command}' was not rejected on a closed NCR");
        Assert.Equal(NcrStatus.Closed, ncr.Status);
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public void A_cancelled_ncr_rejects_every_command(string command, Action<NonConformance> act)
    {
        var ncr = Ncrs.Cancelled();

        var exception = Record.Exception(() => act(ncr));

        Assert.True(exception is DomainException, $"'{command}' was not rejected on a cancelled NCR");
        Assert.Equal(NcrStatus.Cancelled, ncr.Status);
    }
}
