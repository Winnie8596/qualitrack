using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Domain.Tests;

public sealed class FailedCommandTests
{
    public static TheoryData<string, Func<NonConformance>, Action<NonConformance>> FailingCommands => new()
    {
        { "assign: critical due too late", () => Ncrs.Open(Severity.Critical), ncr => ncr.AssignInvestigator(Ncrs.Investigator, Ncrs.RaisedAt.AddDays(8), Ncrs.Day(1, Ncrs.Manager)) },
        { "plan fix: second action blank", () => Ncrs.Investigating(), ncr => ncr.PlanFix("Root cause", ["Grind out and re-weld", " "], Ncrs.Day(2, Ncrs.Investigator)) },
        { "complete action: wrong user", Ncrs.ActionInProgress, ncr => ncr.CompleteAction(ncr.Actions[0].Id, Ncrs.Day(3, Ncrs.Manager)) },
        { "submit: action incomplete", Ncrs.ActionInProgress, ncr => ncr.SubmitForVerification(Ncrs.Day(3, Ncrs.Investigator)) },
        { "reject: no reason", Ncrs.PendingVerification, ncr => ncr.RejectVerification(" ", Ncrs.Day(4, Ncrs.Manager)) },
        { "cancel: no reason", () => Ncrs.Open(), ncr => ncr.Cancel("", Ncrs.Day(1, Ncrs.Manager)) },
    };

    [Theory]
    [MemberData(nameof(FailingCommands))]
    public void A_failed_command_leaves_the_ncr_unchanged(string command, Func<NonConformance> build, Action<NonConformance> act)
    {
        var ncr = build();
        var before = Snapshot.Of(ncr);

        var exception = Record.Exception(() => act(ncr));

        Assert.True(exception is DomainException, $"'{command}' should have failed with a DomainException");
        Assert.Equal(before, Snapshot.Of(ncr));
    }

    private sealed record Snapshot(
        NcrStatus Status,
        UserId? InvestigatorId,
        DateTimeOffset? DueDate,
        string? RootCause,
        string Actions,
        int HistoryCount)
    {
        public static Snapshot Of(NonConformance ncr) => new(
            ncr.Status,
            ncr.InvestigatorId,
            ncr.DueDate,
            ncr.RootCause,
            string.Join(" | ", ncr.Actions.Select(action => $"{action.Id}:{action.Description}:{action.Completed}")),
            ncr.History.Count);
    }
}
