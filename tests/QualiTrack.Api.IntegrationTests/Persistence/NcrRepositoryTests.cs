using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;
using QualiTrack.Infrastructure.Persistence;

namespace QualiTrack.Api.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class NcrRepositoryTests(PostgresFixture database)
{
    private static readonly DateTimeOffset _raisedAt = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly UserId _investigator = new(Guid.NewGuid());

    [Fact]
    public async Task An_added_ncr_is_found_by_id_after_the_unit_of_work_saves()
    {
        var ncr = Raised();
        await using (var context = database.CreateContext())
        {
            new NcrRepository(context).Add(ncr);
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = database.CreateContext();
        var found = await new NcrRepository(readContext).GetByIdAsync(ncr.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(ncr.Number, found.Number);
    }

    [Fact]
    public async Task An_unknown_id_finds_nothing()
    {
        await using var context = database.CreateContext();

        var found = await new NcrRepository(context).GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task Changes_to_a_loaded_ncr_are_saved_including_inside_its_actions()
    {
        var ncr = Raised();
        ncr.AssignInvestigator(_investigator, _raisedAt.AddDays(7), Day(1));
        ncr.PlanFix("Wrong electrode for the steel grade", ["Grind out the weld"], Day(2));
        await SaveNewAsync(ncr);

        await using (var context = database.CreateContext())
        {
            var loaded = await new NcrRepository(context).GetByIdAsync(ncr.Id, CancellationToken.None);
            Assert.NotNull(loaded);
            loaded.CompleteAction(loaded.Actions[0].Id, Day(3));
            loaded.SubmitForVerification(Day(3));
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = database.CreateContext();
        var reloaded = await new NcrRepository(readContext).GetByIdAsync(ncr.Id, CancellationToken.None);
        Assert.NotNull(reloaded);
        Assert.Equal(NcrStatus.PendingVerification, reloaded.Status);
        Assert.Equal(Day(3), reloaded.Actions[0].Completed);
        Assert.Equal(NcrStatus.PendingVerification, reloaded.History[^1].To);
    }

    private NonConformance Raised() =>
        NonConformance.Raise(
            database.NextNumber(),
            new NcrDetails("Weld crack on beam B4", "Hairline crack along the fillet weld.", "Bay 3", Severity.Major),
            new AuditStamp(new UserId(Guid.NewGuid()), _raisedAt));

    private async Task SaveNewAsync(NonConformance ncr)
    {
        await using var context = database.CreateContext();
        context.NonConformances.Add(ncr);
        await context.SaveChangesAsync();
    }

    private static AuditStamp Day(int daysAfterRaised) => new(_investigator, _raisedAt.AddDays(daysAfterRaised));
}
