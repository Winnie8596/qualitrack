using Microsoft.EntityFrameworkCore;
using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Api.IntegrationTests.Persistence;

public sealed class NcrPersistenceTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset _raisedAt = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly UserId _inspector = new(Guid.NewGuid());
    private static readonly UserId _investigator = new(Guid.NewGuid());
    private static readonly UserId _manager = new(Guid.NewGuid());

    [Fact]
    public async Task A_saved_ncr_loads_back_with_its_actions_and_history_in_order()
    {
        var ncr = RejectedOnceThenResubmitted();
        await using (var writeContext = database.CreateContext())
        {
            writeContext.NonConformances.Add(ncr);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = database.CreateContext();
        var loaded = await readContext.NonConformances.SingleAsync(candidate => candidate.Id == ncr.Id);

        Assert.Equal(ncr.Number, loaded.Number);
        Assert.Equal(ncr.Details, loaded.Details);
        Assert.Equal(ncr.Raised, loaded.Raised);
        Assert.Equal(ncr.Status, loaded.Status);
        Assert.Equal(ncr.InvestigatorId, loaded.InvestigatorId);
        Assert.Equal(ncr.DueDate, loaded.DueDate);
        Assert.Equal(ncr.RootCause, loaded.RootCause);
        Assert.Equal(
            ncr.Actions.Select(action => (action.Id, action.Description, action.Added, action.Completed)),
            loaded.Actions.Select(action => (action.Id, action.Description, action.Added, action.Completed)));
        Assert.Equal(ncr.History, loaded.History);
    }

    // Every part filled in: two actions (one completed late), and a rejection note in the history
    private static NonConformance RejectedOnceThenResubmitted()
    {
        var ncr = NonConformance.Raise(
            NcrNumber.From(2026, 1),
            new NcrDetails("Weld crack on beam B4", "Hairline crack along the fillet weld.", "Bay 3", Severity.Critical),
            new AuditStamp(_inspector, _raisedAt));
        ncr.AssignInvestigator(_investigator, _raisedAt.AddDays(7), Day(1, _manager));
        ncr.PlanFix("Wrong electrode for the steel grade", ["Grind out the weld"], Day(2, _investigator));
        ncr.CompleteAction(ncr.Actions[0].Id, Day(3, _investigator));
        ncr.SubmitForVerification(Day(3, _investigator));
        ncr.RejectVerification("Porosity still visible", Day(4, _manager));
        ncr.AddCorrectiveAction("Re-weld with E7018 and X-ray", Day(4, _investigator));
        ncr.CompleteAction(ncr.Actions[1].Id, Day(5, _investigator));
        ncr.SubmitForVerification(Day(5, _investigator));
        return ncr;
    }

    private static AuditStamp Day(int daysAfterRaised, UserId user) => new(user, _raisedAt.AddDays(daysAfterRaised));
}
