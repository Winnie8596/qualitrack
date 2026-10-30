using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QualiTrack.Api.Http;

namespace QualiTrack.Api.IntegrationTests.Ncrs;

[Collection(ApiCollection.Name)]
public sealed class NcrEndpointsTests(QualiTrackApiFactory api)
{
    [Fact]
    public async Task An_inspector_who_raises_an_ncr_gets_201_and_can_open_it()
    {
        var (client, inspectorId) = await SignedInAsync(Inspector);

        var raised = await client.PostAsJsonAsync("/api/ncrs", ValidRequest());
        var opened = await client.GetAsync(raised.Headers.Location);

        Assert.Equal(HttpStatusCode.Created, raised.StatusCode);
        var id = (await raised.Content.ReadFromJsonAsync<RaisedNcr>())!.Id;
        Assert.Equal($"/api/ncrs/{id}", raised.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, opened.StatusCode);
        var ncr = await opened.Content.ReadFromJsonAsync<NcrDetail>();
        Assert.NotNull(ncr);
        Assert.Equal(id, ncr.Id);
        Assert.Matches(@"^NCR-\d{4}-\d{4}$", ncr.Number);
        Assert.Equal(("Burr on flange", "Sharp burr on the mating face.", "Line 3", "Major"),
            (ncr.Title, ncr.Description, ncr.Location, ncr.Severity));
        Assert.Equal("Open", ncr.Status);
        Assert.Equal(inspectorId, ncr.RaisedBy);
        Assert.Empty(ncr.Actions);
    }

    [Theory]
    [InlineData("title")]
    [InlineData("description")]
    [InlineData("location")]
    [InlineData("severity")]
    public async Task Raising_without_a_required_field_gets_400_naming_that_field_and_saves_nothing(string field)
    {
        var (client, _) = await SignedInAsync(Inspector);
        var marker = Guid.NewGuid().ToString();
        var request = ValidRequest(description: marker);
        request[field] = field == "severity" ? null : "   ";

        var response = await client.PostAsJsonAsync("/api/ncrs", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.Contains(field, problem!.Errors.Keys);
        Assert.Equal(0, await CountNcrsDescribedAsAsync(marker));
    }

    [Theory]
    [InlineData("Catastrophic")]
    [InlineData(1)]
    public async Task Raising_with_a_severity_that_is_not_one_of_the_three_names_gets_400(object severity)
    {
        var (client, _) = await SignedInAsync(Inspector);
        var request = ValidRequest();
        request["severity"] = severity;

        var response = await client.PostAsJsonAsync("/api/ncrs", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Raising_without_signing_in_gets_401()
    {
        var response = await Client().PostAsJsonAsync("/api/ncrs", ValidRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Investigator)]
    [InlineData(QaManager)]
    public async Task Only_an_inspector_can_raise_an_ncr(string email)
    {
        var (client, _) = await SignedInAsync(email);

        var response = await client.PostAsJsonAsync("/api/ncrs", ValidRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Opening_an_ncr_without_signing_in_gets_401()
    {
        var location = await RaiseAsync();

        var response = await Client().GetAsync(location);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Opening_an_ncr_that_does_not_exist_gets_404()
    {
        var (client, _) = await SignedInAsync(QaManager);

        var response = await client.GetAsync($"/api/ncrs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Opening_an_ncr_by_an_id_that_is_not_a_guid_gets_404()
    {
        var (client, _) = await SignedInAsync(QaManager);

        var response = await client.GetAsync("/api/ncrs/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(Investigator)]
    [InlineData(null)]
    public async Task An_ncr_the_caller_may_not_see_gets_the_same_404_as_one_that_does_not_exist(string? email)
    {
        var location = await RaiseAsync();
        var (client, _) = await SignedInAsync(email ?? await CreateInspectorAsync());

        var response = await client.GetAsync(location);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_qa_manager_can_open_any_ncr()
    {
        var location = await RaiseAsync();
        var (client, _) = await SignedInAsync(QaManager);

        var response = await client.GetAsync(location);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task An_inspector_lists_only_the_ncrs_they_raised_newest_first()
    {
        await RaiseAsync();
        var (client, _) = await SignedInAsync(await CreateInspectorAsync());
        var first = await RaiseAsync(client);
        var second = await RaiseAsync(client);

        var response = await client.GetAsync("/api/ncrs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<NcrPage>();
        Assert.Equal([IdOf(second), IdOf(first)], page!.Items.Select(item => item.Id));
        Assert.Equal(1, page.Page);
        Assert.False(page.HasNextPage);
        Assert.Equal(("Burr on flange", "Major", "Open", false), (page.Items[0].Title, page.Items[0].Severity, page.Items[0].Status, page.Items[0].IsOverdue));
    }

    [Fact]
    public async Task The_list_takes_its_filters_sort_and_page_from_the_query_string()
    {
        var (client, _) = await SignedInAsync(await CreateInspectorAsync());
        var critical = await RaiseAsync(client, severity: "Critical");
        await RaiseAsync(client, severity: "Minor");

        var response = await client.GetAsync(
            "/api/ncrs?status=Open&severity=Critical&overdueOnly=false&sortBy=DueDate&direction=Ascending&page=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<NcrPage>();
        Assert.Equal([IdOf(critical)], page!.Items.Select(item => item.Id));
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("status=Lost")]
    [InlineData("sortBy=Title")]
    public async Task Listing_with_a_query_value_that_is_not_allowed_gets_400(string queryString)
    {
        var (client, _) = await SignedInAsync(QaManager);

        var response = await client.GetAsync($"/api/ncrs?{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Listing_without_signing_in_gets_401()
    {
        var response = await Client().GetAsync("/api/ncrs");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private const string Inspector = "inspector@qualitrack.demo";
    private const string Investigator = "investigator@qualitrack.demo";
    private const string QaManager = "qamanager@qualitrack.demo";
    private const string Password = "Correct-Horse-9";

    // The session cookie is Secure, so the client only sends it back over HTTPS
    private static readonly Uri _https = new("https://localhost");

    private HttpClient Client() => api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = _https });

    // A dictionary rather than a record, so a test can blank a field or give it a value of the wrong type
    private static Dictionary<string, object?> ValidRequest(
        string description = "Sharp burr on the mating face.", string severity = "Major") =>
        new()
        {
            ["title"] = "Burr on flange",
            ["description"] = description,
            ["location"] = "Line 3",
            ["severity"] = severity,
        };

    private async Task<(HttpClient Client, Guid UserId)> SignedInAsync(string email)
    {
        var client = Client();
        var password = email.EndsWith("@qualitrack.demo", StringComparison.Ordinal) ? QualiTrackApiFactory.DemoPassword : Password;
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();

        // As the web app does: the token belongs to the session, so it is fetched after logging in
        var antiforgery = await client.GetFromJsonAsync<AntiforgeryToken>("/api/auth/antiforgery");
        client.DefaultRequestHeaders.Add(AntiforgeryProtection.HeaderName, antiforgery!.Token);

        return (client, (await login.Content.ReadFromJsonAsync<SignedInUser>())!.Id);
    }

    private async Task<Uri> RaiseAsync() => await RaiseAsync((await SignedInAsync(Inspector)).Client);

    private static async Task<Uri> RaiseAsync(HttpClient client, string severity = "Major")
    {
        var response = await client.PostAsJsonAsync("/api/ncrs", ValidRequest(severity: severity));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return response.Headers.Location!;
    }

    private static Guid IdOf(Uri location) => Guid.Parse(location.OriginalString.Split('/')[^1]);

    // An inspector other than the demo one, to show inspectors only see the NCRs they raised
    private async Task<string> CreateInspectorAsync()
    {
        _ = api.Server;
        var email = $"{Guid.NewGuid()}@qualitrack.test";
        await using var scope = api.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = new IdentityUser<Guid> { UserName = email, Email = email };
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, "Inspector")).Succeeded);
        return email;
    }

    private async Task<int> CountNcrsDescribedAsAsync(string description)
    {
        await using var context = api.CreateDbContext();
        return await context.NonConformances.CountAsync(ncr => ncr.Details.Description == description);
    }

    private sealed record SignedInUser(Guid Id);

    private sealed record AntiforgeryToken(string Token);

    private sealed record RaisedNcr(Guid Id);

    private sealed record NcrPage(IReadOnlyList<NcrListItem> Items, int Page, bool HasNextPage);

    private sealed record NcrListItem(Guid Id, string Title, string Severity, string Status, bool IsOverdue);

    private sealed record NcrDetail(
        Guid Id,
        string Number,
        string Title,
        string Description,
        string Location,
        string Severity,
        string Status,
        Guid RaisedBy,
        IReadOnlyList<object> Actions);
}
