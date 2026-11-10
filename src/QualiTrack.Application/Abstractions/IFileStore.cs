namespace QualiTrack.Application.Abstractions;

/// <summary>Holds the optional NCR photo (A1). Local disk in v1; Blob Storage can replace it without touching handlers.</summary>
public interface IFileStore
{
    /// <summary>Stores <paramref name="content"/> under <paramref name="key"/>, replacing anything already there.</summary>
    Task SaveAsync(Stream content, string key, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken);
}
