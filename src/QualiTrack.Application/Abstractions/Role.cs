namespace QualiTrack.Application.Abstractions;

/// <summary>A user has exactly one role in v1 (PRD §4). Handlers use it to decide who may call a use case.</summary>
public enum Role
{
    Inspector,
    Investigator,
    QaManager,
}
