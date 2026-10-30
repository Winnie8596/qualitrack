using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using QualiTrack.Api.IntegrationTests.Ncrs;

namespace QualiTrack.Api.IntegrationTests.Dashboard;

[Collection(ApiCollection.Name)]
public sealed class DashboardEndpointTests(QualiTrackApiFactory factory)
{
    private readonly NcrApi _api = new(factory);

    [Fact]
    public async Task A_qa_manager_gets_unfinished_ncrs_for_every_severity_the_overdue_count_and_the_average()
    {
        var (manager, _) = await _api.SignedInAsync(NcrApi.QaManager);

        var response = await manager.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardResponse>();
        Assert.Equal(["Minor", "Major", "Critical"], dashboard!.UnfinishedBySeverity.Select(row => row.Severity));
        Assert.True(dashboard.OverdueCount >= 0);
    }

    [Fact]
    public async Task Raising_an_ncr_shows_on_the_dashboard_at_once_rather_than_when_the_cache_expires()
    {
        var (manager, _) = await _api.SignedInAsync(NcrApi.QaManager);
        var before = await CriticalCountAsync(manager);

        await _api.RaiseAsync(severity: "Critical");

        Assert.Equal(before + 1, await CriticalCountAsync(manager));
    }

    [Theory]
    [InlineData(NcrApi.Inspector, HttpStatusCode.Forbidden)]
    [InlineData(NcrApi.Investigator, HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task Only_a_signed_in_qa_manager_can_see_the_dashboard(string? email, HttpStatusCode expected)
    {
        var client = email is null ? _api.Anonymous() : (await _api.SignedInAsync(email)).Client;

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task With_redis_down_the_dashboard_still_answers_from_the_database()
    {
        // Nothing listens on port 1, so every cache call fails to connect
        await using var host = factory.WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:Redis", "localhost:1"));
        var api = new NcrApi(host);
        var (manager, _) = await api.SignedInAsync(NcrApi.QaManager);
        var timer = Stopwatch.StartNew();

        var response = await manager.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(10), $"Took {timer.Elapsed} with Redis down");
    }

    private static async Task<int> CriticalCountAsync(HttpClient manager)
    {
        var dashboard = await manager.GetFromJsonAsync<DashboardResponse>("/api/dashboard");
        return dashboard!.UnfinishedBySeverity.Single(row => row.Severity == "Critical").Count;
    }

    private sealed record DashboardResponse(IReadOnlyList<SeverityRow> UnfinishedBySeverity, int OverdueCount, double? AverageDaysToClose);

    private sealed record SeverityRow(string Severity, int Count);
}
