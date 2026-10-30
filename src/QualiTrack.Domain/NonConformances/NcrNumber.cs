using System.Globalization;
using System.Text.RegularExpressions;

namespace QualiTrack.Domain.NonConformances;

/// <summary>BR-9: unique per year, formatted NCR-YYYY-NNNN.</summary>
public sealed partial record NcrNumber
{
    private const int MaxSequence = 9999;

    private NcrNumber(string value) => Value = value;

    public string Value { get; }

    public static NcrNumber From(int year, int sequence)
    {
        if (year is < 1000 or > 9999)
        {
            throw DomainException.InvalidInput("NCR year must have four digits.");
        }

        if (sequence is < 1 or > MaxSequence)
        {
            throw DomainException.InvalidInput($"NCR sequence must be between 1 and {MaxSequence}.");
        }

        return new NcrNumber(string.Create(CultureInfo.InvariantCulture, $"NCR-{year:D4}-{sequence:D4}"));
    }

    /// <summary>Reads back a number written by <see cref="From"/>, e.g. when loading from storage.</summary>
    public static NcrNumber Parse(string text)
    {
        var match = Format().Match(text);
        if (!match.Success)
        {
            throw DomainException.InvalidInput($"'{text}' is not an NCR number (NCR-YYYY-NNNN).");
        }

        return From(
            int.Parse(match.Groups["year"].ValueSpan, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["sequence"].ValueSpan, CultureInfo.InvariantCulture));
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^NCR-(?<year>\d{4})-(?<sequence>\d{4})$", RegexOptions.CultureInvariant)]
    private static partial Regex Format();
}
