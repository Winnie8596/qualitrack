using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Tests;

/// <summary>NCRs in the states handler tests start from. The domain's own tests cover every transition.</summary>
internal static class Ncrs
{
    public static readonly DateTimeOffset RaisedAt = new(2026, 10, 28, 9, 0, 0, TimeSpan.Zero);

    private static readonly UserId _inspector = new(Guid.NewGuid());
    private static readonly UserId _manager = new(Guid.NewGuid());

    public static NonConformance Open(Severity severity = Severity.Major) =>
        NonConformance.Raise(
            NcrNumber.From(2026, 1),
            new NcrDetails("Weld crack on beam B4", "Hairline crack along the fillet weld.", "Bay 3", severity),
            new AuditStamp(_inspector, RaisedAt));

    public static NonConformance Investigating(UserId investigator)
    {
        var ncr = Open();
        ncr.AssignInvestigator(investigator, RaisedAt.AddDays(7), new AuditStamp(_manager, RaisedAt.AddHours(1)));
        return ncr;
    }

    /// <summary>Root cause recorded and one corrective action, not yet completed.</summary>
    public static NonConformance ActionInProgress(UserId investigator)
    {
        var ncr = Investigating(investigator);
        ncr.PlanFix("Wrong electrode for the steel grade", ["Grind out the weld"], new AuditStamp(investigator, RaisedAt.AddHours(2)));
        return ncr;
    }

    public static NonConformance PendingVerification(UserId investigator)
    {
        var ncr = ActionInProgress(investigator);
        var stamp = new AuditStamp(investigator, RaisedAt.AddHours(3));
        ncr.CompleteAction(ncr.Actions[0].Id, stamp);
        ncr.SubmitForVerification(stamp);
        return ncr;
    }

    public static NonConformance Cancelled()
    {
        var ncr = Open();
        ncr.Cancel("Raised by mistake", new AuditStamp(_manager, RaisedAt.AddHours(1)));
        return ncr;
    }
}
