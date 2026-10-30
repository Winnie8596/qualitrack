using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

namespace QualiTrack.Api.IntegrationTests.Ncrs;

/// <summary>C1 verify and C2 reject, and the whole lifecycle from raising to closing over HTTP.</summary>
[Collection(ApiCollection.Name)]
public sealed class VerificationEndpointsTests(QualiTrackApiFactory factory)
{
    private readonly NcrApi _api = new(factory);

    [Fact]
    public async Task A_qa_manager_who_verifies_the_fix_gets_204_and_the_ncr_is_closed()
    {
        var id = await _api.PendingVerificationAsync();
        var (manager, _) = await _api.SignedInAsync(NcrApi.QaManager);

        var response = await manager.PostAsync($"/api/ncrs/{id}/verify", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("Closed", (await _api.GetAsync(id)).Status);
    }

    [Fact]
    public async Task A_qa_manager_who_rejects_the_fix_gets_204_and_the_actions_are_back_in_progress_with_the_reason()
    {
        var id = await _api.PendingVerificationAsync();
        var (manager, managerId) = await _api.SignedInAsync(NcrApi.QaManager);

        var response = await manager.PostAsJsonAsync($"/api/ncrs/{id}/reject", new { reason = "Burr still on the inner edge" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var ncr = await _api.GetAsync(id);
        Assert.Equal("ActionInProgress", ncr.Status);
        Assert.Equal(new NcrStatusChange("PendingVerification", "ActionInProgress", managerId, "Burr still on the inner edge"), ncr.History[^1]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task Rejecting_without_a_reason_gets_400_naming_the_reason(string? reason)
    {
        var id = await _api.PendingVerificationAsync();
        var (manager, _) = await _api.SignedInAsync(NcrApi.QaManager);

        var response = await manager.PostAsJsonAsync($"/api/ncrs/{id}/reject", new { reason });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.Contains("reason", problem!.Errors.Keys);
    }

    [Theory]
    [InlineData(NcrApi.Inspector)]
    [InlineData(NcrApi.Investigator)]
    public async Task Only_a_qa_manager_can_verify_or_reject(string email)
    {
        var id = await _api.PendingVerificationAsync();
        var (client, _) = await _api.SignedInAsync(email);

        var verify = await client.PostAsync($"/api/ncrs/{id}/verify", null);
        var reject = await client.PostAsJsonAsync($"/api/ncrs/{id}/reject", new { reason = "Not good enough" });

        Assert.Equal(HttpStatusCode.Forbidden, verify.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, reject.StatusCode);
    }

    [Fact]
    public async Task Verifying_or_rejecting_without_signing_in_gets_401()
    {
        var client = _api.Anonymous();
        var id = Guid.NewGuid();

        var verify = await client.PostAsync($"/api/ncrs/{id}/verify", null);
        var reject = await client.PostAsJsonAsync($"/api/ncrs/{id}/reject", new { reason = "Not good enough" });

        Assert.Equal(HttpStatusCode.Unauthorized, verify.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, reject.StatusCode);
    }

    [Fact]
    public async Task Verifying_or_rejecting_an_ncr_that_does_not_exist_gets_404()
    {
        var (manager, _) = await _api.SignedInAsync(NcrApi.QaManager);
        var id = Guid.NewGuid();

        var verify = await manager.PostAsync($"/api/ncrs/{id}/verify", null);
        var reject = await manager.PostAsJsonAsync($"/api/ncrs/{id}/reject", new { reason = "Not good enough" });

        Assert.Equal(HttpStatusCode.NotFound, verify.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, reject.StatusCode);
    }

    [Fact]
    public async Task Verifying_an_ncr_that_is_not_awaiting_verification_gets_409()
    {
        var id = await _api.ActionInProgressAsync();
        var (manager, _) = await _api.SignedInAsync(NcrApi.QaManager);

        var response = await manager.PostAsync($"/api/ncrs/{id}/verify", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task A_closed_ncr_cannot_be_changed_in_any_way()
    {
        var id = await _api.PendingVerificationAsync();
        var (manager, _) = await _api.SignedInAsync(NcrApi.QaManager);
        await manager.PostAsync($"/api/ncrs/{id}/verify", null);

        HttpResponseMessage[] responses =
        [
            await manager.PostAsync($"/api/ncrs/{id}/verify", null),
            await manager.PostAsJsonAsync($"/api/ncrs/{id}/reject", new { reason = "Second thoughts" }),
            await manager.PostAsJsonAsync($"/api/ncrs/{id}/cancel", new { reason = "Second thoughts" }),
            await manager.PostAsJsonAsync($"/api/ncrs/{id}/assign", NcrApi.AssignRequest(await _api.InvestigatorIdAsync())),
        ];

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Conflict, response.StatusCode));
    }

    [Fact]
    public async Task An_ncr_goes_from_raised_to_closed_through_one_rejection_with_every_step_in_its_history()
    {
        var (inspector, inspectorId) = await _api.SignedInAsync(NcrApi.Inspector);
        var (investigator, investigatorId) = await _api.SignedInAsync(NcrApi.Investigator);
        var (manager, managerId) = await _api.SignedInAsync(NcrApi.QaManager);

        var id = await NcrApi.RaiseAsync(inspector, severity: "Critical");
        await ExpectNoContent(manager.PostAsJsonAsync($"/api/ncrs/{id}/assign", NcrApi.AssignRequest(investigatorId, dueInDays: 6)));
        await ExpectNoContent(investigator.PostAsJsonAsync($"/api/ncrs/{id}/plan-fix", NcrApi.PlanFixRequest()));
        await CompleteEveryActionAsync(investigator, id);
        await ExpectNoContent(investigator.PostAsync($"/api/ncrs/{id}/submit", null));
        await ExpectNoContent(manager.PostAsJsonAsync($"/api/ncrs/{id}/reject", new { reason = "Burr still on the inner edge" }));
        await ExpectNoContent(investigator.PostAsJsonAsync($"/api/ncrs/{id}/actions", new { description = "Deburr the inner edge" }));
        await CompleteEveryActionAsync(investigator, id);
        await ExpectNoContent(investigator.PostAsync($"/api/ncrs/{id}/submit", null));
        await ExpectNoContent(manager.PostAsync($"/api/ncrs/{id}/verify", null));

        var ncr = (await inspector.GetFromJsonAsync<NcrDetail>($"/api/ncrs/{id}"))!;
        Assert.Equal("Closed", ncr.Status);
        Assert.Equal(inspectorId, ncr.RaisedBy);
        Assert.Equal(3, ncr.Actions.Count);
        Assert.All(ncr.Actions, action => Assert.NotNull(action.CompletedAt));
        Assert.Equal(
            [
                ("Open", "Investigating", managerId),
                ("Investigating", "ActionInProgress", investigatorId),
                ("ActionInProgress", "PendingVerification", investigatorId),
                ("PendingVerification", "ActionInProgress", managerId),
                ("ActionInProgress", "PendingVerification", investigatorId),
                ("PendingVerification", "Closed", managerId),
            ],
            ncr.History.Select(change => (change.From, change.To, change.By)));
    }

    private async Task CompleteEveryActionAsync(HttpClient investigator, Guid id)
    {
        foreach (var action in (await _api.GetAsync(id)).Actions.Where(action => action.CompletedAt is null))
        {
            await ExpectNoContent(investigator.PostAsync($"/api/ncrs/{id}/actions/{action.Id}/complete", null));
        }
    }

    private static async Task ExpectNoContent(Task<HttpResponseMessage> request) =>
        Assert.Equal(HttpStatusCode.NoContent, (await request).StatusCode);
}
