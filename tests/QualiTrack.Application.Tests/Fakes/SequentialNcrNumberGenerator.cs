using QualiTrack.Application.Abstractions;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Tests.Fakes;

internal sealed class SequentialNcrNumberGenerator : INcrNumberGenerator
{
    private readonly Dictionary<int, int> _lastSequenceByYear = [];

    public Task<NcrNumber> NextAsync(int year, CancellationToken cancellationToken)
    {
        var sequence = _lastSequenceByYear.GetValueOrDefault(year) + 1;
        _lastSequenceByYear[year] = sequence;
        return Task.FromResult(NcrNumber.From(year, sequence));
    }
}
