using QualiTrack.Api.Authentication;
using QualiTrack.Api.Http;
using QualiTrack.Application.Users;

namespace QualiTrack.Api.Users;

/// <summary>The investigators the assign form picks from (A2). Which role may list them is decided by the handler.</summary>
public static class UserEndpoints
{
    public static IServiceCollection AddUserUseCases(this IServiceCollection services) =>
        services.AddScoped<ListInvestigatorsHandler>();

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/users").RequireAuthorization(Policies.SignedIn).MapGet("/investigators", ListInvestigatorsAsync);
        return endpoints;
    }

    private static async Task<IResult> ListInvestigatorsAsync(ListInvestigatorsHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(cancellationToken)).ToHttpResult(TypedResults.Ok);
}
