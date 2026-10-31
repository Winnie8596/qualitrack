namespace QualiTrack.Domain.NonConformances;

/// <summary>
/// The two formats an NCR photo may be (A1). Recognised from the file's own first bytes, its signature, because a
/// file name or a Content-Type header is whatever the sender chose to say.
/// </summary>
public sealed class PhotoFormat
{
    public const int MaxLength = 5 * 1024 * 1024;

    /// <summary>How many leading bytes <see cref="Recognise"/> needs to see.</summary>
    public const int SignatureLength = 8;

    public static readonly PhotoFormat Jpeg = new("Jpeg", "image/jpeg", "jpg", [0xFF, 0xD8, 0xFF]);

    public static readonly PhotoFormat Png = new("Png", "image/png", "png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

    private static readonly PhotoFormat[] _all = [Jpeg, Png];

    private readonly byte[] _signature;

    private PhotoFormat(string name, string contentType, string extension, byte[] signature)
    {
        Name = name;
        ContentType = contentType;
        Extension = extension;
        _signature = signature;
    }

    public string Name { get; }

    public string ContentType { get; }

    public string Extension { get; }

    public static PhotoFormat Recognise(ReadOnlySpan<byte> header, long length)
    {
        if (length > MaxLength)
        {
            throw DomainException.InvalidInput("A photo can be at most 5 MB.");
        }

        foreach (var format in _all)
        {
            if (header.StartsWith(format._signature))
            {
                return format;
            }
        }

        throw DomainException.InvalidInput("A photo must be a JPEG or PNG image.");
    }

    /// <summary>The format stored under <paramref name="name"/>, e.g. when loading an NCR.</summary>
    public static PhotoFormat FromName(string name) =>
        Array.Find(_all, format => format.Name == name)
        ?? throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown photo format.");

    public override string ToString() => Name;
}
