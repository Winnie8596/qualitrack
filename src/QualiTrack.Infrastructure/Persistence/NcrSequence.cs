namespace QualiTrack.Infrastructure.Persistence;

/// <summary>The last NCR sequence number used in a year (BR-9). A storage detail, not part of the domain.</summary>
internal sealed class NcrSequence
{
    // Named here because NcrNumberGenerator writes raw SQL against this table
    public const string Table = "NcrSequences";
    public const string YearColumn = "Year";
    public const string LastSequenceColumn = "LastSequence";

    public int Year { get; init; }

    public int LastSequence { get; init; }
}
