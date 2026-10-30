using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

namespace QualiTrack.Api.IntegrationTests.Ncrs;

/// <summary>B1 plan the fix, B2 add and complete corrective actions, B3 submit for verification.</summary>
[Collection(ApiCollection.Name)]
public sealed class InvestigationEndpointsTests(QualiTrackApiFactory factory)
{
    private readonly NcrApi _api = new(factory);

    [Fact]
    public async Task The_assigned_investigator_who_plans_the_fix_gets_204_and_the_actions_are_in_progress()
    {
        var id = await _api.InvestigatingAsync();
        var (investigator, _) = await _api.SignedInAsync(NcrApi.Investigator);

        var response = await investigator.PostAsJsonAsync($"/api/ncrs/{id}/plan-fix", NcrApi.PlanFixRequest());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var ncr = await _api.GetAsync(id);
        Assert.Equal("ActionInProgress", ncr.Status);
        Assert.Equal("Deburring step skipped on night shift", ncr.RootCause);
        Assert.Equal(["Deburr the flange", "Add deburring to the shift checklist"], ncr.Actions.Select(action => action.Description));
    }

    [Theory]
    [InlineData("rootCause")]
    [InlineData("actions")]
    public async Task Planning_the_fix_without_a_required_field_gets_400_naming_that_field(string field)
    {
        var id = await _api.InvestigatingAsync();
        var request = NcrApi.PlanFixRequest();
        request.Remove(field);
        var (investigator, _) = await _api.SignedInAsync(NcrApi.Investigator);

        var response = await investigator.PostAsJsonAsync($"/api/ncrs/{id}/plan-fix", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.Contains(field, problem!.Errors.Keys);
    }

    [Fact]
    public async Task Planning_the_fix_with_no_corrective_actions_gets_400()
    {
        var id = await _api.InvestigatingAsync();
        var (investigator, _) = await _api.SignedInAsync(NcrApi.Investigator);

        var response = await investigator.PostAsJsonAsync(
            $"/api/ncrs/{id}/plan-fix", new { rootCause = "Deburring step skipped", actions = Array.Empty<string>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Planning_the_fix_of_an_ncr_nobody_is_investigating_yet_gets_409()
    {
        var id = await _api.RaiseAsync();
        var (investigator, _) = await _api.SignedInAsync(NcrApi.Investigator);

        var response = await investigator.PostAsJsonAsync($"/api/ncrs/{id}/plan-fix", NcrApi.PlanFixRequest());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task An_investigator_who_is_not_assigned_gets_403_for_every_investigation_step()
    {
        var investigating = await _api.InvestigatingAsync();
        var inProgress = await _api.ActionInProgressAsync();
        var actionId = (await _api.GetAsync(inProgress)).Actions[0].Id;
        var (other, _) = await _api.SignedInAsync(await _api.CreateUserAsync("Investigator"));

        HttpResponseMessage[] responses =
        [
            await other.PostAsJsonAsync($"/api/ncrs/{investigating}/plan-fix", NcrApi.PlanFixRequest()),
            await other.PostAsJsonAsync($"/api/ncrs/{inProgress}/actions", new { description = "Re-train the night shift" }),
            await other.PostAsync($"/api/ncrs/{inProgress}/actions/{actionId}/complete", null),
            await other.PostAsync($"/api/ncrs/{inProgress}/submit", null),
        ];

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
    }

    [Theory]
    [InlineData(NcrApi.Inspector)]
    [InlineData(NcrApi.QaManager)]
    public async Task Only_an_investigator_can_work_on_the_fix(string email)
    {
        var id = await _api.ActionInProgressAsync();
        var (client, _) = await _api.SignedInAsync(email);

        var response = await client.PostAsJsonAsync($"/api/ncrs/{id}/actions", new { description = "Re-train the night shift" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Investigation_steps_without_signing_in_get_401()
    {
        var id = Guid.NewGuid();
        var client = _api.Anonymous();

        HttpResponseMessage[] responses =
        [
            await client.PostAsJsonAsync($"/api/ncrs/{id}/plan-fix", NcrApi.PlanFixRequest()),
            await client.PostAsJsonAsync($"/api/ncrs/{id}/actions", new { description = "Re-train the night shift" }),
            await client.PostAsync($"/api/ncrs/{id}/actions/{Guid.NewGuid()}/complete", null),
            await client.PostAsync($"/api/ncrs/{id}/submit", null),
        ];

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
    }

    [Fact]
    public async Task Investigation_steps_on_an_ncr_that_does_not_exist_get_404()
    {
        var id = Guid.NewGuid();
        var (investigator, _) = await _api.SignedInAsync(NcrApi.Investigator);

        HttpResponseMessage[] responses =
        [
            await investigator.PostAsJsonAsync($"/api/ncrs/{id}/plan-fix", NcrApi.PlanFixRequest()),
            await investigator.PostAsJsonAsync($"/api/ncrs/{id}/actions", new { description = "Re-train the night shift" }),
            await investigator.PostAsync($"/api/ncrs/{id}/actions/{Guid.NewGuid()}/complete", null),
            await investigator.PostAsync($"/api/ncrs/{id}/submit", null),
        ];

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
    }

    [Fact]
    public async Task Adding_a_corrective_action_while_actions_are_in_progress_gets_204_and_it_is_listed()
    {
        var id = await _api.ActionInProgressAsync();
        var (investigator, _) = await _api.SignedInAsync(NcrApi.Investigator);

        var response = await investigator.PostAsJsonAsync($"/api/ncrs/{id}/actions", new { description = "Re-train the night shift" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("Re-train the night shift", (await _api.GetAsync(id)).Actions[^1].Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task Adding_a_corrective_action_without_a_description_gets_400_naming_it(string? description)
    {
        var id = await _api.ActionInProgressAsync();
        var (investigator, _) = await _api.SignedInAsync(NcrApi.Investigator);

        var response = await investigator.PostAsJsonAsync($"/api/ncrs/{id}/actions", new { description });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.Contains("description", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Adding_a_corrective_action_before_the_fix_is_planned_gets_409()
    {
        var id = await _api.InvestigatingAsync();
        var (investigator, _) = await _api.SignedInAsync(NcrApi.Investigator);

        var response = await investigator.PostAsJsonAsync($"/api/ncrs/{id}/actions", new { description = "Re-train the night shift" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Completing_a_corrective_action_gets_204_and_records_when()
    {
        var id = await _api.ActionInProgressAsync();
        var action = (await _api.GetAsync(id)).Actions[0];
        var (investigator, _) = await _api.SignedInAsync(NcrApi.Investigator);

        var response = await investigator.PostAsync($"/api/ncrs/{id}/actions/{action.Id}/complete", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull((await _api.GetAsync(id)).Actions[0].CompletedAt);
    }

    [Fact]
    public async Task Completing_a_corrective_action_that_is_not_on_the_ncr_gets_400()
    {
        var id = await _api.ActionInProgressAsync();
        var (investigator, _) = await _api.SignedInAsync(NcrApi.Investigator);

        var response = await investigator.PostAsync($"/api/ncrs/{id}/actions/{Guid.NewGuid()}/complete", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submitting_once_every_action_is_complete_gets_204_and_the_ncr_awaits_verification()
    {
        var id = await _api.PendingVerificationAsync();

        var ncr = await _api.GetAsync(id);

        Assert.Equal("PendingVerification", ncr.Status);
        Assert.Equal(["Investigating", "ActionInProgress", "PendingVerification"], ncr.History.Select(change => change.To));
    }

    [Fact]
    public async Task Submitting_with_an_action_not_yet_completed_gets_409()
    {
        var id = await _api.ActionInProgressAsync();
        var (investigator, _) = await _api.SignedInAsync(NcrApi.Investigator);

        var response = await investigator.PostAsync($"/api/ncrs/{id}/submit", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
