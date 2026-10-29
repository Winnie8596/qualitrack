using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Domain.Tests;

public sealed class PlanningTheFixTests
{
    private const string RootCause = "Wrong electrode used for the steel grade";

    [Fact]
    public void Planning_the_fix_records_root_cause_and_actions()
    {
        var ncr = Ncrs.Investigating();

        ncr.PlanFix(RootCause, ["Grind out and re-weld", "Retrain welders on WPS-12"], Ncrs.Day(2, Ncrs.Investigator));

        Assert.Equal(NcrStatus.ActionInProgress, ncr.Status);
        Assert.Equal(RootCause, ncr.RootCause);
        Assert.Equal(["Grind out and re-weld", "Retrain welders on WPS-12"], ncr.Actions.Select(action => action.Description));
        Assert.All(ncr.Actions, action => Assert.False(action.IsCompleted));
    }

    [Fact]
    public void Only_the_assigned_investigator_can_plan_the_fix()
    {
        var ncr = Ncrs.Investigating();

        var exception = Record.Exception(() => ncr.PlanFix(RootCause, ["Grind out and re-weld"], Ncrs.Day(2, Ncrs.Manager)));

        Assert.IsType<DomainException>(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Root_cause_is_required(string rootCause)
    {
        var ncr = Ncrs.Investigating();

        var exception = Record.Exception(() => ncr.PlanFix(rootCause, ["Grind out and re-weld"], Ncrs.Day(2, Ncrs.Investigator)));

        Assert.IsType<DomainException>(exception);
    }

    [Fact]
    public void Planning_the_fix_needs_at_least_one_corrective_action()
    {
        var ncr = Ncrs.Investigating();

        var exception = Record.Exception(() => ncr.PlanFix(RootCause, [], Ncrs.Day(2, Ncrs.Investigator)));

        Assert.IsType<DomainException>(exception);
    }

    [Fact]
    public void Cannot_plan_the_fix_before_an_investigator_is_assigned()
    {
        var ncr = Ncrs.Open();

        var exception = Record.Exception(() => ncr.PlanFix(RootCause, ["Grind out and re-weld"], Ncrs.Day(2, Ncrs.Investigator)));

        Assert.IsType<DomainException>(exception);
    }
}
