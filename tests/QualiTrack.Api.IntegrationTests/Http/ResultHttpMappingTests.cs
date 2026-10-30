using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using QualiTrack.Api.Http;
using QualiTrack.Application;

namespace QualiTrack.Api.IntegrationTests.Http;

public sealed class ResultHttpMappingTests
{
    [Theory]
    [InlineData(ErrorKind.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorKind.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorKind.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorKind.Conflict, StatusCodes.Status409Conflict)]
    public void Each_kind_of_failure_becomes_a_problem_with_its_status_code(ErrorKind kind, int statusCode)
    {
        Result result = new Error(kind, "What went wrong.");

        var problem = Assert.IsType<ProblemHttpResult>(result.ToHttpResult());

        Assert.Equal(statusCode, problem.StatusCode);
        Assert.Equal(statusCode, problem.ProblemDetails.Status);
        Assert.Equal("What went wrong.", problem.ProblemDetails.Detail);
        Assert.False(string.IsNullOrEmpty(problem.ProblemDetails.Title));
    }

    [Fact]
    public void A_failure_with_a_value_type_becomes_the_same_problem()
    {
        Result<Guid> result = Error.NotFound("No such NCR.");

        var problem = Assert.IsType<ProblemHttpResult>(result.ToHttpResult(Results.Ok));

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public void A_success_without_a_value_is_204() =>
        Assert.IsType<NoContent>(Result.Success().ToHttpResult());

    [Fact]
    public void A_success_with_a_value_is_answered_the_way_the_endpoint_chooses()
    {
        var id = Guid.NewGuid();
        Result<Guid> result = id;

        var created = Assert.IsType<Created<Guid>>(result.ToHttpResult(value => TypedResults.Created($"/api/ncrs/{value}", value)));

        Assert.Equal(id, created.Value);
    }
}
