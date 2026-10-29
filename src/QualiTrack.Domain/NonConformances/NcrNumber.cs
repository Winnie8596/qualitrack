using System.Globalization;

namespace QualiTrack.Domain.NonConformances;

/// <summary>BR-9: unique per year, formatted NCR-YYYY-NNNN.</summary>
public sealed record NcrNumber
{
    private const int MaxSequence = 9999;

    private NcrNumber(string value) => Value = value;

    public string Value { get; }

    public static NcrNumber From(int year, int sequence)
    {
        if (year is < 1000 or > 9999)
        {
            throw new DomainException("NCR year must have four digits.");
        }

        if (sequence is < 1 or > MaxSequence)
        {
            throw new DomainException($"NCR sequence must be between 1 and {MaxSequence}.");
        }

        return new NcrNumber(string.Create(CultureInfo.InvariantCulture, $"NCR-{year:D4}-{sequence:D4}"));
    }

    public override string ToString() => Value;
}
