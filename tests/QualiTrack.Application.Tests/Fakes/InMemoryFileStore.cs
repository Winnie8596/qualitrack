using QualiTrack.Application.Abstractions;

namespace QualiTrack.Application.Tests.Fakes;

internal sealed class InMemoryFileStore : IFileStore
{
    private readonly Dictionary<string, byte[]> _files = [];

    public IReadOnlyDictionary<string, byte[]> Files => _files;

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken)
    {
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken);
        _files[fileName] = copy.ToArray();
        return fileName;
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream(_files[key], writable: false));
}
