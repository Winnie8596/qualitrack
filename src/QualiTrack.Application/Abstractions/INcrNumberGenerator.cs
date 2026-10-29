using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Abstractions;

public interface INcrNumberGenerator
{
    /// <summary>BR-9: the next number in <paramref name="year"/>'s sequence, unique even under concurrent calls.</summary>
    Task<NcrNumber> NextAsync(int year, CancellationToken cancellationToken);
}
