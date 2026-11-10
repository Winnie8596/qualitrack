using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Api.DemoData;

/// <summary>The three demo users, as the people in every demo NCR's story.</summary>
internal sealed record DemoCast(UserId Inspector, UserId Investigator, UserId QaManager);

/// <summary>
/// Takes a demo NCR to its stage through the aggregate's own methods, each stamped with a past time, so seeded
/// NCRs obey every business rule and carry the same history a real one would (ADR 0003 is what makes this possible).
/// </summary>
internal static class DemoNcrBuilder
{
    private static readonly TimeSpan _untilTriaged = TimeSpan.FromHours(4);
    private static readonly TimeSpan _untilPlanned = TimeSpan.FromDays(1);
    private static readonly TimeSpan _untilFirstActionDone = TimeSpan.FromDays(2);
    private static readonly TimeSpan _untilSubmitted = _untilFirstActionDone + TimeSpan.FromHours(2);
    private static readonly TimeSpan _untilReviewed = TimeSpan.FromDays(3);

    /// <summary>The oldest stamp any stage needs is this long after the NCR was raised.</summary>
    public static TimeSpan TimeNeededFor(DemoStage stage) => stage switch
    {
        DemoStage.Open => TimeSpan.Zero,
        DemoStage.Cancelled or DemoStage.Investigating => _untilTriaged,
        DemoStage.ActionInProgress => _untilFirstActionDone,
        DemoStage.PendingVerification => _untilSubmitted,
        DemoStage.ReworkAfterRejection or DemoStage.Closed => _untilReviewed,
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown demo stage."),
    };

    public static NonConformance Build(DemoNcr demo, NcrNumber number, DateTimeOffset raisedAt, DemoCast cast)
    {
        var details = new NcrDetails(demo.Title, demo.Description, demo.Location, demo.Severity);
        var ncr = NonConformance.Raise(number, details, new AuditStamp(cast.Inspector, raisedAt));

        if (demo.Stage != DemoStage.Open)
        {
            Triage(ncr, demo, cast);
        }

        if (demo.Stage >= DemoStage.ActionInProgress)
        {
            WorkOnTheFix(ncr, demo, cast);
        }

        if (demo.Stage >= DemoStage.ReworkAfterRejection)
        {
            Review(ncr, demo, cast);
        }

        return ncr;
    }

    private static void Triage(NonConformance ncr, DemoNcr demo, DemoCast cast)
    {
        var triaged = new AuditStamp(cast.QaManager, ncr.Raised.At + _untilTriaged);
        if (demo.Stage == DemoStage.Cancelled)
        {
            ncr.Cancel(Required(demo.Note, demo), triaged);
            return;
        }

        ncr.AssignInvestigator(cast.Investigator, triaged.At + TimeToDue(demo.Severity), triaged);
    }

    // The first action is always done; the second stays open unless the NCR went on to verification
    private static void WorkOnTheFix(NonConformance ncr, DemoNcr demo, DemoCast cast)
    {
        var fix = Required(demo.Fix, demo);
        AuditStamp At(TimeSpan sinceRaised) => new(cast.Investigator, ncr.Raised.At + sinceRaised);

        ncr.PlanFix(fix.RootCause, [fix.FirstAction, fix.SecondAction], At(_untilPlanned));
        ncr.CompleteAction(ncr.Actions[0].Id, At(_untilFirstActionDone));

        if (demo.Stage >= DemoStage.ReworkAfterRejection)
        {
            ncr.CompleteAction(ncr.Actions[1].Id, At(_untilSubmitted));
            ncr.SubmitForVerification(At(_untilSubmitted));
        }
    }

    private static void Review(NonConformance ncr, DemoNcr demo, DemoCast cast)
    {
        var reviewed = new AuditStamp(cast.QaManager, ncr.Raised.At + _untilReviewed);
        switch (demo.Stage)
        {
            case DemoStage.ReworkAfterRejection:
                ncr.RejectVerification(Required(demo.Note, demo), reviewed);
                break;
            case DemoStage.Closed:
                ncr.Verify(reviewed);
                break;
            default:
                // Pending verification: submitted, and still waiting for the QA manager
                break;
        }
    }

    // Critical stays inside BR-2's seven days from raising; the others are long enough that some demo NCRs are
    // still in time and short enough that the older unfinished ones are overdue
    private static TimeSpan TimeToDue(Severity severity) => severity switch
    {
        Severity.Critical => TimeSpan.FromDays(5),
        Severity.Major => TimeSpan.FromDays(10),
        _ => TimeSpan.FromDays(21),
    };

    private static T Required<T>(T? value, DemoNcr demo)
        where T : class =>
        value ?? throw new InvalidOperationException($"Demo NCR '{demo.Title}' is missing what its stage {demo.Stage} needs.");
}
