using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Api.DemoData;

/// <summary>How far through its lifecycle a demo NCR has got by the time the demo starts.</summary>
internal enum DemoStage
{
    Open,
    Cancelled,
    Investigating,
    ActionInProgress,
    ReworkAfterRejection,
    PendingVerification,
    Closed,
}

internal sealed record DemoFix(string RootCause, string FirstAction, string SecondAction);

/// <summary>
/// One made-up finding from a metal fabrication and assembly plant, and where its story has reached. It is open
/// until one of the methods below says otherwise. Note is why it was cancelled or why its fix was rejected.
/// </summary>
internal sealed record DemoNcr(string Title, string Description, string Location)
{
    public required Severity Severity { get; init; }

    public DemoStage Stage { get; private init; } = DemoStage.Open;

    public DemoFix? Fix { get; private init; }

    public string? Note { get; private init; }

    public DemoNcr CancelledBecause(string reason) => this with { Stage = DemoStage.Cancelled, Note = reason };

    public DemoNcr Investigating() => this with { Stage = DemoStage.Investigating };

    public DemoNcr InProgress(DemoFix fix) => this with { Stage = DemoStage.ActionInProgress, Fix = fix };

    public DemoNcr SentBackForRework(DemoFix fix, string rejectionReason) =>
        this with { Stage = DemoStage.ReworkAfterRejection, Fix = fix, Note = rejectionReason };

    public DemoNcr PendingVerification(DemoFix fix) => this with { Stage = DemoStage.PendingVerification, Fix = fix };

    public DemoNcr Closed(DemoFix fix) => this with { Stage = DemoStage.Closed, Fix = fix };
}
