namespace QualiTrack.Application.Tests;

public sealed class ResultTests
{
    [Fact]
    public void A_successful_result_has_no_error_to_read()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void A_failed_result_carries_its_error()
    {
        var error = Error.Conflict("NCR is already closed.");

        Result result = error;

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void A_successful_result_carries_its_value()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void The_value_of_a_failed_result_cannot_be_read()
    {
        Result<int> result = Error.NotFound("NCR not found.");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Theory]
    [InlineData(ErrorKind.Validation)]
    [InlineData(ErrorKind.NotFound)]
    [InlineData(ErrorKind.Forbidden)]
    [InlineData(ErrorKind.Conflict)]
    public void Each_error_factory_sets_its_kind(ErrorKind kind)
    {
        var error = kind switch
        {
            ErrorKind.Validation => Error.Validation("message"),
            ErrorKind.NotFound => Error.NotFound("message"),
            ErrorKind.Forbidden => Error.Forbidden("message"),
            ErrorKind.Conflict => Error.Conflict("message"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        Assert.Equal(kind, error.Kind);
    }
}
