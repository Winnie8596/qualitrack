using System.ComponentModel.DataAnnotations;
using QualiTrack.Api.Authentication;
using QualiTrack.Api.Http;
using QualiTrack.Application.NonConformances;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Api.Ncrs;

/// <summary>
/// What the raise form sends (A1). Every field is nullable so a missing one reaches validation and comes back as
/// an error on that field, rather than failing JSON binding with no field named. The domain still enforces BR-1;
/// this checks the same fields at the edge so the form can say which one is wrong.
/// </summary>
public sealed record RaiseNcrRequest(
    [Required] string? Title,
    [Required] string? Description,
    [Required] string? Location,
    [Required] Severity? Severity)
{
    // "!" is safe: validation has rejected a request with any of these missing before the endpoint runs
    public RaiseNcrCommand ToCommand() => new(Title!, Description!, Location!, Severity!.Value);
}

public sealed record RaisedNcrResponse(Guid Id);

/// <summary>Raise an NCR and open one (A1, D2). Which role may do either is decided by the handler.</summary>
public static class NcrEndpoints
{
    public static IServiceCollection AddNcrUseCases(this IServiceCollection services) =>
        services.AddScoped<RaiseNcrHandler>().AddScoped<GetNcrByIdHandler>();

    public static IEndpointRouteBuilder MapNcrEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var ncrs = endpoints.MapGroup("/api/ncrs").RequireAuthorization(Policies.SignedIn).RequireAntiforgeryToken();
        ncrs.MapPost("/", RaiseAsync);
        ncrs.MapGet("/{id:guid}", GetByIdAsync);
        return endpoints;
    }

    private static async Task<IResult> RaiseAsync(
        RaiseNcrRequest request, RaiseNcrHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        return result.ToHttpResult(id => TypedResults.Created($"/api/ncrs/{id}", new RaisedNcrResponse(id)));
    }

    private static async Task<IResult> GetByIdAsync(Guid id, GetNcrByIdHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetNcrByIdQuery(id), cancellationToken);
        return result.ToHttpResult(TypedResults.Ok);
    }
}
