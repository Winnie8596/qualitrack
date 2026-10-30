using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QualiTrack.Api.Http;

namespace QualiTrack.Api.IntegrationTests.Http;

[Collection(ApiCollection.Name)]
public sealed class CorrelationIdTests(QualiTrackApiFactory api)
{
    private static readonly Uri _https = new("https://localhost");

    [Fact]
    public async Task A_request_without_a_correlation_id_gets_a_new_one_on_the_response()
    {
        var response = await Client().GetAsync("/health");

        Assert.Matches("^[0-9a-f]{32}$", CorrelationIdOf(response));
    }

    [Fact]
    public async Task A_correlation_id_sent_by_the_caller_comes_back_unchanged()
    {
        var response = await SendAsync("/health", "web-7f3a9c");

        Assert.Equal("web-7f3a9c", CorrelationIdOf(response));
    }

    [Theory]
    [InlineData("has spaces and\ttabs")]
    [InlineData("a-very-long-correlation-id-that-goes-on-well-past-sixty-four-characters-x")]
    public async Task A_correlation_id_that_is_not_short_and_plain_is_replaced_so_it_cannot_forge_log_lines(string sent)
    {
        var response = await SendAsync("/health", sent);

        Assert.Matches("^[0-9a-f]{32}$", CorrelationIdOf(response));
    }

    [Fact]
    public async Task Each_request_is_logged_once_with_its_method_path_status_duration_and_correlation_id()
    {
        var logs = new CapturingLoggerProvider();
        using var host = api.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton<ILoggerProvider>(logs)));
        var client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = _https });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/ncrs");
        request.Headers.Add(CorrelationId.HeaderName, "trace-me-1");

        await client.SendAsync(request);

        var entry = Assert.Single(logs.Entries, entry => entry.Scopes.Contains(("CorrelationId", "trace-me-1")) && entry.Category.Contains("HttpLogging", StringComparison.Ordinal));
        Assert.Equal("GET", entry.Value("Method"));
        Assert.Equal("/api/ncrs", entry.Value("Path"));
        Assert.Equal("401", entry.Value("StatusCode"));
        Assert.NotNull(entry.Value("Duration"));
    }

    private HttpClient Client() => api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = _https });

    private async Task<HttpResponseMessage> SendAsync(string path, string correlationId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation(CorrelationId.HeaderName, correlationId);
        return await Client().SendAsync(request);
    }

    private static string CorrelationIdOf(HttpResponseMessage response) =>
        Assert.Single(response.Headers.GetValues(CorrelationId.HeaderName));

    private sealed record LogEntry(
        string Category, IReadOnlyList<KeyValuePair<string, object?>> State, IReadOnlyList<(string, string?)> Scopes)
    {
        public string? Value(string key) => State.FirstOrDefault(pair => pair.Key == key).Value?.ToString();
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();

        public ConcurrentQueue<LogEntry> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new Logger(categoryName, this);

        public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

        public void Dispose()
        {
        }

        private sealed class Logger(string category, CapturingLoggerProvider provider) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => provider._scopes.Push(state);

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                List<(string, string?)> scopes = [];
                provider._scopes.ForEachScope(
                    (scope, found) =>
                    {
                        if (scope is IEnumerable<KeyValuePair<string, object>> pairs)
                        {
                            found.AddRange(pairs.Select(pair => (pair.Key, pair.Value?.ToString())));
                        }
                    },
                    scopes);
                var values = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];
                provider.Entries.Enqueue(new LogEntry(category, values, scopes));
            }
        }
    }
}
