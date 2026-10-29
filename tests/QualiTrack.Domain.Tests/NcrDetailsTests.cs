using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Domain.Tests;

public sealed class NcrDetailsTests
{
    private const string ValidTitle = "Cracked weld on beam B4";
    private const string ValidDescription = "Hairline crack along the fillet weld, approx. 40 mm.";
    private const string ValidLocation = "Bay 3, Level 2";

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Title_is_required(string title)
    {
        var exception = Record.Exception(() => new NcrDetails(title, ValidDescription, ValidLocation, Severity.Major));

        Assert.IsType<DomainException>(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Description_is_required(string description)
    {
        var exception = Record.Exception(() => new NcrDetails(ValidTitle, description, ValidLocation, Severity.Major));

        Assert.IsType<DomainException>(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Location_is_required(string location)
    {
        var exception = Record.Exception(() => new NcrDetails(ValidTitle, ValidDescription, location, Severity.Major));

        Assert.IsType<DomainException>(exception);
    }

    [Fact]
    public void Severity_must_be_minor_major_or_critical()
    {
        var unknown = (Severity)99;

        var exception = Record.Exception(() => new NcrDetails(ValidTitle, ValidDescription, ValidLocation, unknown));

        Assert.IsType<DomainException>(exception);
    }

    [Fact]
    public void Valid_details_keep_their_values()
    {
        var details = new NcrDetails(ValidTitle, ValidDescription, ValidLocation, Severity.Critical);

        Assert.Equal(ValidTitle, details.Title);
        Assert.Equal(ValidDescription, details.Description);
        Assert.Equal(ValidLocation, details.Location);
        Assert.Equal(Severity.Critical, details.Severity);
    }
}
