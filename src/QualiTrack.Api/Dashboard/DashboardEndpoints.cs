using QualiTrack.Api.Authentication;
using QualiTrack.Api.Http;
using QualiTrack.Application.Dashboard;

namespace QualiTrack.Api.Dashboard;

/// <summary>The QA manager's dashboard (D3). Which role may see it is decided by the handler.</summary>
public static class DashboardEndpoints
{
    public static IServiceCollection AddDashboardUseCases(this IServiceCollection services) =>
        services.AddScoped<GetDashboardHandler>();

    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/dashboard", GetAsync).RequireAuthorization(Policies.SignedIn);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(GetDashboardHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(cancellationToken)).ToHttpResult(TypedResults.Ok);
}
