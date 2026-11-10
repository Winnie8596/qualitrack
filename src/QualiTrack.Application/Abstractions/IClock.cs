namespace QualiTrack.Application.Abstractions;

/// <summary>The domain never reads the time itself; handlers take it from here so tests can fix it.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
