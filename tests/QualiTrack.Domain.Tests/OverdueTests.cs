using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Domain.Tests;

/// <summary>BR-8. The NCRs built by <see cref="Ncrs"/> are due on day 7.</summary>
public sealed class OverdueTests
{
    public static TheoryData<string, Func<NonConformance>, int, bool> Cases => new()
    {
        { "open, no due date yet", () => Ncrs.Open(), 30, false },
        { "investigating, before due", () => Ncrs.Investigating(), 6, false },
        { "investigating, on the due moment", () => Ncrs.Investigating(), 7, false },
        { "investigating, after due", () => Ncrs.Investigating(), 8, true },
        { "action in progress, after due", Ncrs.ActionInProgress, 8, true },
        { "pending verification, after due", Ncrs.PendingVerification, 8, true },
        { "closed, after due", Ncrs.Closed, 8, false },
        { "cancelled, after due", Ncrs.Cancelled, 8, false },
    };

    public static TheoryData<string, Func<NonConformance>, bool> FinishedCases => new()
    {
        { "open", () => Ncrs.Open(), false },
        { "investigating", () => Ncrs.Investigating(), false },
        { "action in progress", Ncrs.ActionInProgress, false },
        { "pending verification", Ncrs.PendingVerification, false },
        { "closed", Ncrs.Closed, true },
        { "cancelled", Ncrs.Cancelled, true },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Ncr_is_overdue_after_its_due_date_unless_finished(string state, Func<NonConformance> build, int daysAfterRaised, bool expected)
    {
        var ncr = build();
        var now = Ncrs.RaisedAt.AddDays(daysAfterRaised);

        var isOverdue = ncr.IsOverdue(now);

        Assert.True(expected == isOverdue, $"{state}: expected IsOverdue to be {expected}");
    }

    [Theory]
    [MemberData(nameof(FinishedCases))]
    public void Only_closed_and_cancelled_ncrs_are_finished(string state, Func<NonConformance> build, bool expected)
    {
        var ncr = build();

        var isFinished = ncr.IsFinished;

        Assert.True(expected == isFinished, $"{state}: expected IsFinished to be {expected}");
    }
}
