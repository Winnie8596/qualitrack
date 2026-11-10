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
/// One made-up finding from a metal fabrication and assembly plant, and where its story has reached.
/// Note is why it was cancelled, or why its fix was rejected; other stages have none.
/// </summary>
internal sealed record DemoNcr(
    DemoStage Stage,
    Severity Severity,
    string Title,
    string Description,
    string Location,
    DemoFix? Fix = null,
    string? Note = null);
