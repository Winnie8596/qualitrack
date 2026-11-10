using QualiTrack.Application.Abstractions;

namespace QualiTrack.Application.Tests.Fakes;

internal sealed class InMemoryFileStore : IFileStore
{
    private readonly Dictionary<string, byte[]> _files = [];

    public IReadOnlyDictionary<string, byte[]> Files => _files;

    public async Task SaveAsync(Stream content, string key, CancellationToken cancellationToken)
    {
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken);
        _files[key] = copy.ToArray();
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream(_files[key], writable: false));
}
