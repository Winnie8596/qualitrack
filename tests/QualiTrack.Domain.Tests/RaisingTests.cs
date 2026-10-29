using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Domain.Tests;

public sealed class RaisingTests
{
    [Fact]
    public void A_new_ncr_starts_open()
    {
        var raised = new AuditStamp(Ncrs.Inspector, Ncrs.RaisedAt);

        var ncr = NonConformance.Raise(NcrNumber.From(2026, 1), Ncrs.Details(), raised);

        Assert.Equal(NcrStatus.Open, ncr.Status);
        Assert.Equal(raised, ncr.Raised);
        Assert.Equal("NCR-2026-0001", ncr.Number.Value);
        Assert.Null(ncr.InvestigatorId);
        Assert.Empty(ncr.History);
    }
}
