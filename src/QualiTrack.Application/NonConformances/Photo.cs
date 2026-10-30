using QualiTrack.Application.Abstractions;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.NonConformances;

/// <summary>The photo's bytes. The API caps the request, so they fit in memory; the domain checks the real length.</summary>
public sealed record AttachPhotoCommand(Guid NcrId, Stream Content);

public sealed record GetNcrPhotoQuery(Guid NcrId);

/// <summary>An open stream of the photo; the caller disposes it.</summary>
public sealed record PhotoView(Stream Content, string ContentType);

/// <summary>Where an NCR's photo is kept: one per NCR, so its id and format name the file.</summary>
internal static class NcrPhotoFile
{
    public static string KeyFor(Guid ncrId, PhotoFormat format) => $"{ncrId}.{format.Extension}";
}

/// <summary>A1: the photo is checked and recorded on the NCR first, then stored, then the NCR is saved.</summary>
public sealed class AttachPhotoHandler(NcrCommandRunner runner, IFileStore files, ICurrentUser currentUser)
{
    public Task<Result> HandleAsync(AttachPhotoCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Role != Role.Inspector)
        {
            return Task.FromResult<Result>(Error.Forbidden("Only an inspector can attach a photo."));
        }

        return runner.RunAsync(
            command.NcrId,
            async (ncr, stamp) =>
            {
                // Read whole, so the format and size are judged on what arrived, not on what the request claimed
                using var photo = new MemoryStream();
                await command.Content.CopyToAsync(photo, cancellationToken);
                var header = photo.GetBuffer().AsSpan(0, (int)Math.Min(photo.Length, PhotoFormat.SignatureLength));
                var format = PhotoFormat.Recognise(header, photo.Length);
                ncr.AttachPhoto(format, stamp);

                photo.Position = 0;
                await files.SaveAsync(photo, NcrPhotoFile.KeyFor(ncr.Id, format), cancellationToken);
                return Result.Success();
            },
            cancellationToken);
    }
}

/// <summary>D2: the photo, for anyone who may see the NCR; for anyone else, as for an NCR with none, NotFound.</summary>
public sealed class GetNcrPhotoHandler(INcrRepository repository, IFileStore files, ICurrentUser currentUser)
{
    public async Task<Result<PhotoView>> HandleAsync(GetNcrPhotoQuery query, CancellationToken cancellationToken)
    {
        var ncr = await repository.GetByIdAsync(query.NcrId, cancellationToken);
        if (ncr is null || !currentUser.CanView(ncr) || ncr.Photo is not { } format)
        {
            return Error.NotFound($"NCR {query.NcrId} has no photo.");
        }

        var content = await files.OpenReadAsync(NcrPhotoFile.KeyFor(ncr.Id, format), cancellationToken);
        return new PhotoView(content, format.ContentType);
    }
}
