using QualiTrack.Application.Abstractions;

namespace QualiTrack.Infrastructure.Storage;

/// <summary>
/// Files in one folder on local disk (a volume in Docker), keyed by file name. PRD §11's default for v1; Blob
/// Storage can replace it behind <see cref="IFileStore"/>.
/// </summary>
public sealed class LocalFileStore(string root) : IFileStore
{
    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken)
    {
        var path = PathOf(fileName);
        Directory.CreateDirectory(root);

        // Written beside the target first, so a reader never sees half a file
        var partial = $"{path}.partial";
        await using (var file = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        File.Move(partial, path, overwrite: true);
        return fileName;
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new FileStream(PathOf(key), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));

    // Keys come from the application, never from a request, but a plain name is still checked: "../" must not
    // turn into a path outside the folder
    private string PathOf(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key != Path.GetFileName(key) || key.Contains('/') || key.Contains('\\') || key.StartsWith('.'))
        {
            throw new ArgumentException($"'{key}' is not a plain file name.", nameof(key));
        }

        return Path.Combine(root, key);
    }
}
