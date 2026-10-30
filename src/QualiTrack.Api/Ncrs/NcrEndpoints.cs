using System.ComponentModel.DataAnnotations;
using QualiTrack.Api.Authentication;
using QualiTrack.Api.Http;
using QualiTrack.Application.Abstractions;
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

/// <summary>Assign an investigator and due date (A2). Nullable for the same reason as <see cref="RaiseNcrRequest"/>.</summary>
public sealed record AssignInvestigatorRequest([Required] Guid? InvestigatorId, [Required] DateTimeOffset? DueDate)
{
    // "!" is safe: validation has rejected a request with either missing before the endpoint runs
    public AssignInvestigatorCommand ToCommand(Guid ncrId) => new(ncrId, InvestigatorId!.Value, DueDate!.Value);
}

/// <summary>A reason, which cancelling (A3) and rejecting (C2) both require (BR-7).</summary>
public sealed record ReasonRequest([Required] string? Reason);

/// <summary>
/// The list's filters, sort and page from the query string (D1), e.g. <c>?status=Open&amp;sortBy=DueDate</c>.
/// Enum values are their names, as in JSON; a value that is not one of them is a 400.
/// </summary>
public sealed record ListNcrsRequest(
    NcrStatus? Status,
    Severity? Severity,
    bool? OverdueOnly,
    NcrSortField? SortBy,
    SortDirection? Direction,
    int? Page)
{
    public ListNcrsQuery ToQuery()
    {
        var defaults = new ListNcrsQuery();
        return new(
            Status,
            Severity,
            OverdueOnly ?? defaults.OverdueOnly,
            SortBy ?? defaults.SortBy,
            Direction ?? defaults.Direction,
            Page ?? defaults.Page);
    }
}

/// <summary>Raise an NCR and open one (A1, D2). Which role may do either is decided by the handler.</summary>
public static class NcrEndpoints
{
    public static IServiceCollection AddNcrUseCases(this IServiceCollection services) =>
        services
            .AddScoped<NcrCommandRunner>()
            .AddScoped<RaiseNcrHandler>()
            .AddScoped<GetNcrByIdHandler>()
            .AddScoped<ListNcrsHandler>()
            .AddScoped<AssignInvestigatorHandler>()
            .AddScoped<CancelNcrHandler>();

    public static IEndpointRouteBuilder MapNcrEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var ncrs = endpoints.MapGroup("/api/ncrs").RequireAuthorization(Policies.SignedIn).RequireAntiforgeryToken();
        ncrs.MapGet("/", ListAsync);
        ncrs.MapPost("/", RaiseAsync);
        ncrs.MapGet("/{id:guid}", GetByIdAsync);
        ncrs.MapPost("/{id:guid}/assign", AssignAsync);
        ncrs.MapPost("/{id:guid}/cancel", CancelAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] ListNcrsRequest request, ListNcrsHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToQuery(), cancellationToken);
        return result.ToHttpResult(TypedResults.Ok);
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

    private static async Task<IResult> AssignAsync(
        Guid id, AssignInvestigatorRequest request, AssignInvestigatorHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(request.ToCommand(id), cancellationToken)).ToHttpResult();

    // "!" is safe: validation has rejected a request with no reason before the endpoint runs
    private static async Task<IResult> CancelAsync(
        Guid id, ReasonRequest request, CancelNcrHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new CancelNcrCommand(id, request.Reason!), cancellationToken)).ToHttpResult();
}
