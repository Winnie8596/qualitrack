using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace QualiTrack.Api.Observability;

/// <summary>
/// OpenTelemetry for the API: a trace per request with a child span for every SQL command EF Core sends, and
/// the request-duration and connection-pool metrics. Logs say what happened, metrics how often and how fast,
/// and a trace where one request spent its time.
/// </summary>
public static class Telemetry
{
    private const string ServiceName = "qualitrack-api";

    // The standard OpenTelemetry variable, so the same setting works for any collector: the Aspire Dashboard in
    // docker compose, Azure Monitor's collector in production
    private const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

    private const string ApiPath = "/api";

    // Npgsql's own meter: open and busy connections, command duration
    private const string NpgsqlMeter = "Npgsql";

    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var telemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing => tracing
                // Health probes every few seconds and a dozen static files per page load would bury the requests
                // that do the work
                .AddAspNetCoreInstrumentation(options => options.Filter = IsApiRequest)
                // EF Core's queries, traced where they leave the process. The SQL text is recorded; its parameter
                // values, which can hold personal data, are not.
                .AddNpgsql())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddMeter(NpgsqlMeter));

        // Collected either way, so the cost is the same everywhere; sent only where there is somewhere to send it
        if (!string.IsNullOrWhiteSpace(configuration[OtlpEndpointKey]))
        {
            telemetry.UseOtlpExporter();
        }

        return services;
    }

    private static bool IsApiRequest(HttpContext context) =>
        context.Request.Path.StartsWithSegments(ApiPath, StringComparison.Ordinal);
}
