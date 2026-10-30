using Microsoft.EntityFrameworkCore;

namespace QualiTrack.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class StartupMigrationTests(QualiTrackApiFactory api)
{
    [Fact]
    public async Task In_development_the_app_brings_the_database_up_to_date_when_it_starts()
    {
        _ = api.Server;

        await using var context = api.CreateDbContext();
        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }
}
