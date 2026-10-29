namespace QualiTrack.Application.Abstractions;

/// <summary>Holds the optional NCR photo (A1). Local disk in v1; Blob Storage can replace it without touching handlers.</summary>
public interface IFileStore
{
    /// <returns>A key that identifies the stored file.</returns>
    Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken);
}
