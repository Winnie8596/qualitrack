using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Domain.Tests;

public sealed class VerifyingTests
{
    [Fact]
    public void Investigator_cannot_verify_their_own_fix()
    {
        var ncr = Ncrs.PendingVerification();

        var exception = Record.Exception(() => ncr.Verify(Ncrs.Day(4, Ncrs.Investigator)));

        Assert.IsType<DomainException>(exception);
    }

    [Fact]
    public void Another_user_verifying_closes_the_ncr()
    {
        var ncr = Ncrs.PendingVerification();
        var verified = Ncrs.Day(4, Ncrs.Manager);

        ncr.Verify(verified);

        Assert.Equal(NcrStatus.Closed, ncr.Status);
        Assert.Equal(new StatusChange(NcrStatus.PendingVerification, NcrStatus.Closed, verified, null), ncr.History[^1]);
    }

    [Fact]
    public void Cannot_verify_before_the_fix_is_submitted()
    {
        var ncr = Ncrs.ActionInProgress();

        var exception = Record.Exception(() => ncr.Verify(Ncrs.Day(4, Ncrs.Manager)));

        Assert.IsType<DomainException>(exception);
    }

    [Fact]
    public void Rejected_verification_returns_ncr_to_action_in_progress_with_reason()
    {
        var ncr = Ncrs.PendingVerification();
        var rejected = Ncrs.Day(4, Ncrs.Manager);

        ncr.RejectVerification("Weld still shows porosity on UT scan", rejected);

        Assert.Equal(NcrStatus.ActionInProgress, ncr.Status);
        Assert.Equal(
            new StatusChange(NcrStatus.PendingVerification, NcrStatus.ActionInProgress, rejected, "Weld still shows porosity on UT scan"),
            ncr.History[^1]);
    }

    [Fact]
    public void Investigator_cannot_reject_their_own_fix()
    {
        var ncr = Ncrs.PendingVerification();

        var exception = Record.Exception(() => ncr.RejectVerification("Not good enough", Ncrs.Day(4, Ncrs.Investigator)));

        Assert.IsType<DomainException>(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejecting_requires_a_reason(string reason)
    {
        var ncr = Ncrs.PendingVerification();

        var exception = Record.Exception(() => ncr.RejectVerification(reason, Ncrs.Day(4, Ncrs.Manager)));

        Assert.IsType<DomainException>(exception);
    }
}
