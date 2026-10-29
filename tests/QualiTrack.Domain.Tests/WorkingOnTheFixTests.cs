using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Domain.Tests;

public sealed class WorkingOnTheFixTests
{
    [Fact]
    public void Investigator_can_add_an_action_while_the_fix_is_in_progress()
    {
        var ncr = Ncrs.ActionInProgress();

        ncr.AddCorrectiveAction("Inspect the other welds on B4", Ncrs.Day(3, Ncrs.Investigator));

        Assert.Equal(2, ncr.Actions.Count);
        Assert.Equal("Inspect the other welds on B4", ncr.Actions[1].Description);
    }

    [Fact]
    public void Only_the_assigned_investigator_can_add_an_action()
    {
        var ncr = Ncrs.ActionInProgress();

        var exception = Record.Exception(() => ncr.AddCorrectiveAction("Inspect the other welds", Ncrs.Day(3, Ncrs.Manager)));

        Assert.Equal(DomainErrorKind.NotPermitted, Assert.IsType<DomainException>(exception).Kind);
    }

    [Fact]
    public void Cannot_add_an_action_once_submitted_for_verification()
    {
        var ncr = Ncrs.PendingVerification();

        var exception = Record.Exception(() => ncr.AddCorrectiveAction("Inspect the other welds", Ncrs.Day(3, Ncrs.Investigator)));

        Assert.Equal(DomainErrorKind.InvalidState, Assert.IsType<DomainException>(exception).Kind);
    }

    [Fact]
    public void Investigator_can_complete_an_action()
    {
        var ncr = Ncrs.ActionInProgress();
        var completed = Ncrs.Day(3, Ncrs.Investigator);

        ncr.CompleteAction(ncr.Actions[0].Id, completed);

        Assert.Equal(completed, ncr.Actions[0].Completed);
    }

    [Fact]
    public void Only_the_assigned_investigator_can_complete_an_action()
    {
        var ncr = Ncrs.ActionInProgress();

        var exception = Record.Exception(() => ncr.CompleteAction(ncr.Actions[0].Id, Ncrs.Day(3, Ncrs.Manager)));

        Assert.Equal(DomainErrorKind.NotPermitted, Assert.IsType<DomainException>(exception).Kind);
    }

    [Fact]
    public void Completing_an_action_that_is_not_on_this_ncr_is_rejected()
    {
        var ncr = Ncrs.ActionInProgress();

        var exception = Record.Exception(() => ncr.CompleteAction(Guid.NewGuid(), Ncrs.Day(3, Ncrs.Investigator)));

        Assert.Equal(DomainErrorKind.InvalidInput, Assert.IsType<DomainException>(exception).Kind);
    }

    [Fact]
    public void Cannot_submit_while_a_corrective_action_is_incomplete()
    {
        var ncr = Ncrs.ActionInProgress();
        ncr.AddCorrectiveAction("Inspect the other welds on B4", Ncrs.Day(3, Ncrs.Investigator));
        ncr.CompleteAction(ncr.Actions[0].Id, Ncrs.Day(3, Ncrs.Investigator));

        var exception = Record.Exception(() => ncr.SubmitForVerification(Ncrs.Day(3, Ncrs.Investigator)));

        Assert.Equal(DomainErrorKind.InvalidState, Assert.IsType<DomainException>(exception).Kind);
        Assert.Equal(NcrStatus.ActionInProgress, ncr.Status);
    }

    [Fact]
    public void Submitting_with_every_action_completed_awaits_verification()
    {
        var ncr = Ncrs.ActionInProgress();
        ncr.CompleteAction(ncr.Actions[0].Id, Ncrs.Day(3, Ncrs.Investigator));

        ncr.SubmitForVerification(Ncrs.Day(3, Ncrs.Investigator));

        Assert.Equal(NcrStatus.PendingVerification, ncr.Status);
    }

    [Fact]
    public void Only_the_assigned_investigator_can_submit_for_verification()
    {
        var ncr = Ncrs.ActionInProgress();
        ncr.CompleteAction(ncr.Actions[0].Id, Ncrs.Day(3, Ncrs.Investigator));

        var exception = Record.Exception(() => ncr.SubmitForVerification(Ncrs.Day(3, Ncrs.Manager)));

        Assert.Equal(DomainErrorKind.NotPermitted, Assert.IsType<DomainException>(exception).Kind);
    }
}
