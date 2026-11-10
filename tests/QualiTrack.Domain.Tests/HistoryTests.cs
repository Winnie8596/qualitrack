using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Domain.Tests;

public sealed class HistoryTests
{
    [Fact]
    public void Each_status_change_is_recorded_with_who_and_when()
    {
        var ncr = Ncrs.PendingVerification();

        Assert.Equal(
            [
                new StatusChange(NcrStatus.Open, NcrStatus.Investigating, Ncrs.Day(1, Ncrs.Manager), null),
                new StatusChange(NcrStatus.Investigating, NcrStatus.ActionInProgress, Ncrs.Day(2, Ncrs.Investigator), null),
                new StatusChange(NcrStatus.ActionInProgress, NcrStatus.PendingVerification, Ncrs.Day(3, Ncrs.Investigator), null),
            ],
            ncr.History);
    }
}
