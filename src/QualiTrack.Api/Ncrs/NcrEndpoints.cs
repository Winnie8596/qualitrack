using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
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

/// <summary>Plan the fix (B1): the root cause and the first corrective actions. The domain requires at least one.</summary>
public sealed record PlanFixRequest([Required] string? RootCause, [Required] IReadOnlyList<string>? Actions)
{
    // "!" is safe: validation has rejected a request with either missing before the endpoint runs
    public PlanFixCommand ToCommand(Guid ncrId) => new(ncrId, RootCause!, Actions!);
}

/// <summary>One more corrective action (B2), e.g. after a rejected verification.</summary>
public sealed record AddCorrectiveActionRequest([Required] string? Description);

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
    // The photo's 5 MB (checked by the domain) and room for the multipart headers; anything bigger is refused
    // before it is read, so a huge upload costs no memory
    private const long PhotoRequestSizeLimit = PhotoFormat.MaxLength + (64 * 1024);

    public static IServiceCollection AddNcrUseCases(this IServiceCollection services) =>
        services
            .AddScoped<NcrCommandRunner>()
            .AddScoped<RaiseNcrHandler>()
            .AddScoped<GetNcrByIdHandler>()
            .AddScoped<ListNcrsHandler>()
            .AddScoped<AssignInvestigatorHandler>()
            .AddScoped<CancelNcrHandler>()
            .AddScoped<PlanFixHandler>()
            .AddScoped<AddCorrectiveActionHandler>()
            .AddScoped<CompleteCorrectiveActionHandler>()
            .AddScoped<SubmitForVerificationHandler>()
            .AddScoped<VerifyNcrHandler>()
            .AddScoped<RejectVerificationHandler>()
            .AddScoped<AttachPhotoHandler>()
            .AddScoped<GetNcrPhotoHandler>();

    public static IEndpointRouteBuilder MapNcrEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var ncrs = endpoints.MapGroup("/api/ncrs").RequireAuthorization(Policies.SignedIn).RequireAntiforgeryToken();
        ncrs.MapGet("/", ListAsync);
        ncrs.MapPost("/", RaiseAsync);
        ncrs.MapGet("/{id:guid}", GetByIdAsync);
        ncrs.MapPost("/{id:guid}/assign", AssignAsync);
        ncrs.MapPost("/{id:guid}/cancel", CancelAsync);
        ncrs.MapPost("/{id:guid}/plan-fix", PlanFixAsync);
        ncrs.MapPost("/{id:guid}/actions", AddActionAsync);
        ncrs.MapPost("/{id:guid}/actions/{actionId:guid}/complete", CompleteActionAsync);
        ncrs.MapPost("/{id:guid}/submit", SubmitAsync);
        ncrs.MapPost("/{id:guid}/verify", VerifyAsync);
        ncrs.MapPost("/{id:guid}/reject", RejectAsync);

        // The framework adds its own antiforgery check to endpoints that read a form, which needs middleware this
        // app does not use; the group's filter above checks the token on this endpoint as on every other write
        ncrs.MapPost("/{id:guid}/photo", AttachPhotoAsync)
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(PhotoRequestSizeLimit));
        ncrs.MapGet("/{id:guid}/photo", GetPhotoAsync);
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

    private static async Task<IResult> PlanFixAsync(
        Guid id, PlanFixRequest request, PlanFixHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(request.ToCommand(id), cancellationToken)).ToHttpResult();

    // "!" is safe: validation has rejected a request with no description before the endpoint runs
    private static async Task<IResult> AddActionAsync(
        Guid id, AddCorrectiveActionRequest request, AddCorrectiveActionHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new AddCorrectiveActionCommand(id, request.Description!), cancellationToken)).ToHttpResult();

    private static async Task<IResult> CompleteActionAsync(
        Guid id, Guid actionId, CompleteCorrectiveActionHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new CompleteCorrectiveActionCommand(id, actionId), cancellationToken)).ToHttpResult();

    private static async Task<IResult> SubmitAsync(
        Guid id, SubmitForVerificationHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new SubmitForVerificationCommand(id), cancellationToken)).ToHttpResult();

    private static async Task<IResult> VerifyAsync(Guid id, VerifyNcrHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new VerifyNcrCommand(id), cancellationToken)).ToHttpResult();

    // "!" is safe: validation has rejected a request with no reason before the endpoint runs
    private static async Task<IResult> RejectAsync(
        Guid id, ReasonRequest request, RejectVerificationHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new RejectVerificationCommand(id, request.Reason!), cancellationToken)).ToHttpResult();

    private static async Task<IResult> AttachPhotoAsync(
        Guid id, IFormFile? photo, AttachPhotoHandler handler, CancellationToken cancellationToken)
    {
        if (photo is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["photo"] = ["Choose a photo."] });
        }

        await using var content = photo.OpenReadStream();
        return (await handler.HandleAsync(new AttachPhotoCommand(id, content), cancellationToken)).ToHttpResult();
    }

    // The stream is disposed by the result once it has been sent
    private static async Task<IResult> GetPhotoAsync(Guid id, GetNcrPhotoHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new GetNcrPhotoQuery(id), cancellationToken))
            .ToHttpResult(photo => TypedResults.Stream(photo.Content, photo.ContentType));
}
