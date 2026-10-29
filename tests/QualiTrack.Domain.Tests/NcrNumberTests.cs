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

        Assert.IsType<DomainException>(exception);
    }

    [Theory]
    [InlineData(999)]
    [InlineData(10_000)]
    public void Ncr_year_must_have_four_digits(int year)
    {
        var exception = Record.Exception(() => NcrNumber.From(year, 1));

        Assert.IsType<DomainException>(exception);
    }
}
