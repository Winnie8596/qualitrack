using QualiTrack.Domain;

namespace QualiTrack.Application.Tests;

public sealed class DomainErrorsTests
{
    public static TheoryData<DomainException, ErrorKind> BrokenRules => new()
    {
        { DomainException.InvalidInput("Title is required."), ErrorKind.Validation },
        { DomainException.InvalidState("NCR must be Open but is Closed."), ErrorKind.Conflict },
        { DomainException.NotPermitted("Only the assigned investigator can do this."), ErrorKind.Forbidden },
    };

    [Theory]
    [MemberData(nameof(BrokenRules))]
    public void Each_kind_of_broken_rule_becomes_its_error_kind_with_the_same_message(DomainException exception, ErrorKind expected)
    {
        var error = DomainErrors.From(exception);

        Assert.Equal(expected, error.Kind);
        Assert.Equal(exception.Message, error.Message);
    }
}
