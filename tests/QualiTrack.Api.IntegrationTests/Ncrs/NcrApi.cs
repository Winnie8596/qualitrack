using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using QualiTrack.Api.Http;

namespace QualiTrack.Api.IntegrationTests.Ncrs;

/// <summary>
/// Signs in as the demo users (or new ones) and drives NCRs through the API the way the web app does, so an
/// endpoint test can set up the state it needs in a line and then check the one call it is about.
/// </summary>
internal sealed class NcrApi(WebApplicationFactory<Program> api)
{
    public const string Inspector = "inspector@qualitrack.demo";
    public const string Investigator = "investigator@qualitrack.demo";
    public const string QaManager = "qamanager@qualitrack.demo";
    public const string Password = "Correct-Horse-9";

    // The session cookie is Secure, so the client only sends it back over HTTPS
    private static readonly Uri _https = new("https://localhost");

    public HttpClient Anonymous() => api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = _https });

    public async Task<(HttpClient Client, Guid UserId)> SignedInAsync(string email)
    {
        var client = Anonymous();
        var password = email.EndsWith("@qualitrack.demo", StringComparison.Ordinal) ? QualiTrackApiFactory.DemoPassword : Password;
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();

        // As the web app does: the token belongs to the session, so it is fetched after logging in
        var antiforgery = await client.GetFromJsonAsync<AntiforgeryToken>("/api/auth/antiforgery");
        client.DefaultRequestHeaders.Add(AntiforgeryProtection.HeaderName, antiforgery!.Token);

        return (client, (await login.Content.ReadFromJsonAsync<SignedInUser>())!.Id);
    }

    /// <summary>A user other than the demo ones, e.g. to show an inspector only sees the NCRs they raised.</summary>
    public async Task<string> CreateUserAsync(string role)
    {
        _ = api.Server;
        var email = $"{Guid.NewGuid()}@qualitrack.test";
        await using var scope = api.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = new IdentityUser<Guid> { UserName = email, Email = email };
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        return email;
    }

    // A dictionary rather than a record, so a test can blank a field or give it a value of the wrong type
    public static Dictionary<string, object?> RaiseRequest(
        string description = "Sharp burr on the mating face.", string severity = "Major") =>
        new()
        {
            ["title"] = "Burr on flange",
            ["description"] = description,
            ["location"] = "Line 3",
            ["severity"] = severity,
        };

    public async Task<Guid> RaiseAsync(string severity = "Major") =>
        await RaiseAsync((await SignedInAsync(Inspector)).Client, severity);

    public static async Task<Guid> RaiseAsync(HttpClient client, string severity = "Major")
    {
        var response = await client.PostAsJsonAsync("/api/ncrs", RaiseRequest(severity: severity));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RaisedNcr>())!.Id;
    }

    /// <summary>Raised, then assigned to the demo investigator, due in five days.</summary>
    public async Task<Guid> InvestigatingAsync()
    {
        var id = await RaiseAsync();
        var (manager, _) = await SignedInAsync(QaManager);
        var response = await manager.PostAsJsonAsync($"/api/ncrs/{id}/assign", AssignRequest(await InvestigatorIdAsync()));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return id;
    }

    /// <summary>Assigned to the demo investigator, who has planned the fix with two corrective actions.</summary>
    public async Task<Guid> ActionInProgressAsync()
    {
        var id = await InvestigatingAsync();
        var (investigator, _) = await SignedInAsync(Investigator);
        var response = await investigator.PostAsJsonAsync($"/api/ncrs/{id}/plan-fix", PlanFixRequest());
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return id;
    }

    /// <summary>Every corrective action completed, and submitted by the demo investigator.</summary>
    public async Task<Guid> PendingVerificationAsync()
    {
        var id = await ActionInProgressAsync();
        var (investigator, _) = await SignedInAsync(Investigator);
        foreach (var action in (await GetAsync(id)).Actions)
        {
            var completed = await investigator.PostAsync($"/api/ncrs/{id}/actions/{action.Id}/complete", null);
            Assert.Equal(HttpStatusCode.NoContent, completed.StatusCode);
        }

        var submitted = await investigator.PostAsync($"/api/ncrs/{id}/submit", null);
        Assert.Equal(HttpStatusCode.NoContent, submitted.StatusCode);
        return id;
    }

    public static Dictionary<string, object?> PlanFixRequest() =>
        new()
        {
            ["rootCause"] = "Deburring step skipped on night shift",
            ["actions"] = new[] { "Deburr the flange", "Add deburring to the shift checklist" },
        };

    public static Dictionary<string, object?> AssignRequest(Guid investigatorId, int dueInDays = 5) =>
        new()
        {
            ["investigatorId"] = investigatorId,
            ["dueDate"] = DateTimeOffset.UtcNow.AddDays(dueInDays),
        };

    public async Task<Guid> InvestigatorIdAsync() => (await SignedInAsync(Investigator)).UserId;

    public async Task<NcrDetail> GetAsync(Guid id)
    {
        var (manager, _) = await SignedInAsync(QaManager);
        return (await manager.GetFromJsonAsync<NcrDetail>($"/api/ncrs/{id}"))!;
    }

    private sealed record SignedInUser(Guid Id);

    private sealed record AntiforgeryToken(string Token);

    private sealed record RaisedNcr(Guid Id);
}

internal sealed record NcrDetail(
    Guid Id,
    string Number,
    string Title,
    string Description,
    string Location,
    string Severity,
    string Status,
    Guid RaisedBy,
    Guid? InvestigatorId,
    DateTimeOffset? DueDate,
    string? RootCause,
    IReadOnlyList<NcrActionDetail> Actions,
    IReadOnlyList<NcrStatusChange> History,
    bool HasPhoto);

internal sealed record NcrActionDetail(Guid Id, string Description, DateTimeOffset? CompletedAt);

internal sealed record NcrStatusChange(string From, string To, Guid By, string? Note);
