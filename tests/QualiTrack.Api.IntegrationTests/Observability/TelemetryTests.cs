using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using QualiTrack.Api.IntegrationTests.Ncrs;

namespace QualiTrack.Api.IntegrationTests.Observability;

/// <summary>
/// The app's own tracing and metrics setup, with a collector added beside whatever it exports to, so the tests
/// see exactly the spans and measurements a real collector would receive. The server ends a request's span and
/// records its duration just after the client has its response, so each test waits for what it expects.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class TelemetryTests(QualiTrackApiFactory api)
{
    private static readonly TimeSpan _patience = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _pollEvery = TimeSpan.FromMilliseconds(25);

    [Fact]
    public async Task A_request_is_traced_together_with_the_sql_it_ran()
    {
        var spans = new SpanCollector();
        using var host = HostExportingTo(spans, []);
        var (manager, _) = await new NcrApi(host).SignedInAsync(NcrApi.QaManager);

        await manager.GetAsync("/api/ncrs");

        var request = await spans.WaitForAsync(span => span.Kind == ActivityKind.Server && Path(span) == "/api/ncrs");
        Assert.Contains(spans.All, span => span.Kind == ActivityKind.Client && span.TraceId == request.TraceId && IsPostgres(span));
    }

    [Fact]
    public async Task Only_api_requests_are_traced_not_health_probes()
    {
        var spans = new SpanCollector();
        using var host = HostExportingTo(spans, []);
        var client = new NcrApi(host).Anonymous();

        await client.GetAsync("/health");
        await client.GetAsync("/api/auth/me");

        // The health request ended first, so its span would be here by the time the later one is
        await spans.WaitForAsync(span => Path(span) == "/api/auth/me");
        Assert.DoesNotContain(spans.All, span => Path(span) == "/health");
    }

    [Fact]
    public async Task How_long_each_request_took_is_measured()
    {
        var metrics = new List<Metric>();
        using var host = HostExportingTo(new SpanCollector(), metrics);
        var meters = host.Services.GetRequiredService<MeterProvider>();

        await new NcrApi(host).Anonymous().GetAsync("/api/auth/me");

        var waiting = Stopwatch.StartNew();
        var names = new HashSet<string>();
        while (!names.Contains("http.server.request.duration") && waiting.Elapsed < _patience)
        {
            await Task.Delay(_pollEvery);
            meters.ForceFlush();
            names.UnionWith(metrics.Select(metric => metric.Name));
        }

        Assert.Contains("http.server.request.duration", names);
    }

    private WebApplicationFactory<Program> HostExportingTo(SpanCollector spans, List<Metric> metrics) =>
        api.WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services
            .ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddProcessor(new SimpleActivityExportProcessor(spans)))
            .ConfigureOpenTelemetryMeterProvider(metering => metering.AddInMemoryExporter(metrics))));

    private static string? Path(Activity span) => span.GetTagItem("url.path") as string;

    private static bool IsPostgres(Activity span) =>
        (span.GetTagItem("db.system.name") ?? span.GetTagItem("db.system")) as string == "postgresql";

    /// <summary>An exporter that keeps every finished span, safe to read while the server is still adding to it.</summary>
    private sealed class SpanCollector : BaseExporter<Activity>
    {
        private readonly ConcurrentQueue<Activity> _spans = new();

        public IReadOnlyCollection<Activity> All => _spans;

        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (var span in batch)
            {
                _spans.Enqueue(span);
            }

            return ExportResult.Success;
        }

        public async Task<Activity> WaitForAsync(Func<Activity, bool> isWanted)
        {
            var waiting = Stopwatch.StartNew();
            while (waiting.Elapsed < _patience)
            {
                if (_spans.FirstOrDefault(isWanted) is { } span)
                {
                    return span;
                }

                await Task.Delay(_pollEvery);
            }

            throw new TimeoutException($"No such span arrived within {_patience.TotalSeconds} s; {_spans.Count} others did.");
        }
    }
}
