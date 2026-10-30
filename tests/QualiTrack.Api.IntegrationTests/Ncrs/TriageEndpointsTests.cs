using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

namespace QualiTrack.Api.IntegrationTests.Ncrs;

/// <summary>A2 assign an investigator and A3 cancel, and the investigator list the assign form picks from.</summary>
[Collection(ApiCollection.Name)]
public sealed class TriageEndpointsTests(QualiTrackApiFactory factory)
{
    private readonly NcrApi _api = new(factory);

    [Fact]
    public async Task A_qa_manager_who_assigns_an_investigator_gets_204_and_the_ncr_is_being_investigated()
    {
        var id = await _api.RaiseAsync();
        var investigatorId = await _api.InvestigatorIdAsync();
        var (manager, _) = await _api.SignedInAsync(NcrApi.QaManager);

        var response = await manager.PostAsJsonAsync($"/api/ncrs/{id}/assign", NcrApi.AssignRequest(investigatorId));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var ncr = await _api.GetAsync(id);
        Assert.Equal("Investigating", ncr.Status);
        Assert.Equal(investigatorId, ncr.InvestigatorId);
        Assert.NotNull(ncr.DueDate);
    }

    [Theory]
    [InlineData("investigatorId")]
    [InlineData("dueDate")]
    public async Task Assigning_without_a_required_field_gets_400_naming_that_field(string field)
    {
        var id = await _api.RaiseAsync();
        var request = NcrApi.AssignRequest(await _api.InvestigatorIdAsync());
        request.Remove(field);
        var (manager, _) = await _api.SignedInAsync(NcrApi.QaManager);

        var response = await manager.PostAsJsonAsync($"/api/ncrs/{id}/assign", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.Contains(field, problem!.Errors.Keys);
    }

    [Fact]
    public async Task Assigning_a_critical_ncr_due_more_than_seven_days_after_it_was_raised_gets_400()
    {
        var id = await _api.RaiseAsync(severity: "Critical");
        var (manager, _) = await _api.SignedInAsync(NcrApi.QaManager);

        var response = await manager.PostAsJsonAsync(
            $"/api/ncrs/{id}/assign", NcrApi.AssignRequest(await _api.InvestigatorIdAsync(), dueInDays: 8));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Open", (await _api.GetAsync(id)).Status);
    }

    [Fact]
    public async Task Assigning_to_a_user_who_is_not_an_investigator_gets_400()
    {
        var id = await _api.RaiseAsync();
        var (manager, managerId) = await _api.SignedInAsync(NcrApi.QaManager);

        var response = await manager.PostAsJsonAsync($"/api/ncrs/{id}/assign", NcrApi.AssignRequest(managerId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Assigning_an_ncr_that_is_no_longer_open_gets_409()
    {
        var id = await _api.InvestigatingAsync();
        var (manager, _) = await _api.SignedInAsync(NcrApi.QaManager);

        var response = await manager.PostAsJsonAsync($"/api/ncrs/{id}/assign", NcrApi.AssignRequest(await _api.InvestigatorIdAsync()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData(NcrApi.Inspector)]
    [InlineData(NcrApi.Investigator)]
    public async Task Only_a_qa_manager_can_assign_or_cancel(string email)
    {
        var id = await _api.RaiseAsync();
        var (client, _) = await _api.SignedInAsync(email);

        var assign = await client.PostAsJsonAsync($"/api/ncrs/{id}/assign", NcrApi.AssignRequest(await _api.InvestigatorIdAsync()));
        var cancel = await client.PostAsJsonAsync($"/api/ncrs/{id}/cancel", new { reason = "Raised by mistake" });

        Assert.Equal(HttpStatusCode.Forbidden, assign.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, cancel.StatusCode);
    }

    [Fact]
    public async Task Assigning_or_cancelling_an_ncr_that_does_not_exist_gets_404()
    {
        var (manager, _) = await _api.SignedInAsync(NcrApi.QaManager);
        var id = Guid.NewGuid();

        var assign = await manager.PostAsJsonAsync($"/api/ncrs/{id}/assign", NcrApi.AssignRequest(await _api.InvestigatorIdAsync()));
        var cancel = await manager.PostAsJsonAsync($"/api/ncrs/{id}/cancel", new { reason = "Raised by mistake" });

        Assert.Equal(HttpStatusCode.NotFound, assign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, cancel.StatusCode);
    }

    [Fact]
    public async Task Assigning_or_cancelling_without_signing_in_gets_401()
    {
        var id = await _api.RaiseAsync();
        var client = _api.Anonymous();

        var assign = await client.PostAsJsonAsync($"/api/ncrs/{id}/assign", NcrApi.AssignRequest(Guid.NewGuid()));
        var cancel = await client.PostAsJsonAsync($"/api/ncrs/{id}/cancel", new { reason = "Raised by mistake" });

        Assert.Equal(HttpStatusCode.Unauthorized, assign.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, cancel.StatusCode);
    }

    [Fact]
    public async Task A_qa_manager_who_cancels_an_ncr_gets_204_and_the_reason_is_in_its_history()
    {
        var id = await _api.RaiseAsync();
        var (manager, managerId) = await _api.SignedInAsync(NcrApi.QaManager);

        var response = await manager.PostAsJsonAsync($"/api/ncrs/{id}/cancel", new { reason = "Raised by mistake" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var ncr = await _api.GetAsync(id);
        Assert.Equal("Cancelled", ncr.Status);
        Assert.Equal(new NcrStatusChange("Open", "Cancelled", managerId, "Raised by mistake"), ncr.History[^1]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Cancelling_without_a_reason_gets_400_naming_the_reason(string? reason)
    {
        var id = await _api.RaiseAsync();
        var (manager, _) = await _api.SignedInAsync(NcrApi.QaManager);

        var response = await manager.PostAsJsonAsync($"/api/ncrs/{id}/cancel", new { reason });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.Contains("reason", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Cancelling_an_ncr_that_is_already_cancelled_gets_409()
    {
        var id = await _api.RaiseAsync();
        var (manager, _) = await _api.SignedInAsync(NcrApi.QaManager);
        await manager.PostAsJsonAsync($"/api/ncrs/{id}/cancel", new { reason = "Raised by mistake" });

        var response = await manager.PostAsJsonAsync($"/api/ncrs/{id}/cancel", new { reason = "Again" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task A_qa_manager_lists_the_investigators_by_id_and_email()
    {
        var investigatorId = await _api.InvestigatorIdAsync();
        var (manager, _) = await _api.SignedInAsync(NcrApi.QaManager);

        var investigators = await manager.GetFromJsonAsync<List<UserSummary>>("/api/users/investigators");

        Assert.Contains(new UserSummary(investigatorId, NcrApi.Investigator), investigators!);
    }

    [Theory]
    [InlineData(NcrApi.Inspector, HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task Only_a_signed_in_qa_manager_can_list_investigators(string? email, HttpStatusCode expected)
    {
        var client = email is null ? _api.Anonymous() : (await _api.SignedInAsync(email)).Client;

        var response = await client.GetAsync("/api/users/investigators");

        Assert.Equal(expected, response.StatusCode);
    }

    private sealed record UserSummary(Guid Id, string Email);
}
