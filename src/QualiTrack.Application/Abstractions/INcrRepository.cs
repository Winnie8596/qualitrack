using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Abstractions;

public interface INcrRepository
{
    /// <returns>The NCR, or <c>null</c> when none has this id; the handler turns that into a NotFound result.</returns>
    Task<NonConformance?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(NonConformance ncr);
}
