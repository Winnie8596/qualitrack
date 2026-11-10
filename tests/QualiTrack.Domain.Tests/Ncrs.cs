using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Domain.Tests;

/// <summary>Builds NCRs in each lifecycle state so tests only spell out what they are about.</summary>
internal static class Ncrs
{
    public static readonly UserId Inspector = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    public static readonly UserId Investigator = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    public static readonly UserId Manager = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));

    public static readonly DateTimeOffset RaisedAt = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

    /// <summary>A stamp for <paramref name="user"/> a number of days after the NCR was raised.</summary>
    public static AuditStamp Day(int daysAfterRaised, UserId user) => new(user, RaisedAt.AddDays(daysAfterRaised));

    public static NcrDetails Details(Severity severity = Severity.Major) =>
        new("Cracked weld on beam B4", "Hairline crack along the fillet weld, approx. 40 mm.", "Bay 3, Level 2", severity);

    public static NonConformance Open(Severity severity = Severity.Major) =>
        NonConformance.Raise(NcrNumber.From(2026, 1), Details(severity), new AuditStamp(Inspector, RaisedAt));

    public static NonConformance Investigating(Severity severity = Severity.Major)
    {
        var ncr = Open(severity);
        ncr.AssignInvestigator(Investigator, RaisedAt.AddDays(7), Day(1, Manager));
        return ncr;
    }

    /// <summary>Root cause recorded and one corrective action, not yet completed.</summary>
    public static NonConformance ActionInProgress()
    {
        var ncr = Investigating();
        ncr.PlanFix("Wrong electrode used for the steel grade", ["Grind out and re-weld"], Day(2, Investigator));
        return ncr;
    }

    public static NonConformance PendingVerification()
    {
        var ncr = ActionInProgress();
        ncr.CompleteAction(ncr.Actions[0].Id, Day(3, Investigator));
        ncr.SubmitForVerification(Day(3, Investigator));
        return ncr;
    }

    public static NonConformance Closed()
    {
        var ncr = PendingVerification();
        ncr.Verify(Day(4, Manager));
        return ncr;
    }

    public static NonConformance Cancelled()
    {
        var ncr = Open();
        ncr.Cancel("Raised twice by mistake", Day(1, Manager));
        return ncr;
    }
}
