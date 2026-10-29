using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Domain.Tests;

public sealed class NcrNumberTests
{
    [Fact]
    public void Ncr_number_is_padded_to_four_digits()
    {
        var number = NcrNumber.From(2026, 7);

        Assert.Equal("NCR-2026-0007", number.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10_000)]
    public void Ncr_sequence_must_fit_in_four_digits(int sequence)
    {
        var exception = Record.Exception(() => NcrNumber.From(2026, sequence));

        Assert.Equal(DomainErrorKind.InvalidInput, Assert.IsType<DomainException>(exception).Kind);
    }

    [Fact]
    public void Ncr_number_is_read_back_from_its_text()
    {
        var number = NcrNumber.Parse("NCR-2026-0042");

        Assert.Equal(NcrNumber.From(2026, 42), number);
    }

    [Theory]
    [InlineData("")]
    [InlineData("NCR-26-0001")]
    [InlineData("ABC-2026-0001")]
    [InlineData("NCR-2026-0000")]
    [InlineData("NCR-2026-00001")]
    public void Text_that_is_not_an_ncr_number_is_rejected(string text)
    {
        var exception = Record.Exception(() => NcrNumber.Parse(text));

        Assert.Equal(DomainErrorKind.InvalidInput, Assert.IsType<DomainException>(exception).Kind);
    }

    [Theory]
    [InlineData(999)]
    [InlineData(10_000)]
    public void Ncr_year_must_have_four_digits(int year)
    {
        var exception = Record.Exception(() => NcrNumber.From(year, 1));

        Assert.Equal(DomainErrorKind.InvalidInput, Assert.IsType<DomainException>(exception).Kind);
    }
}
