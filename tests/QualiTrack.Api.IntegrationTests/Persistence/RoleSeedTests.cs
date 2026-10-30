using Microsoft.EntityFrameworkCore;

namespace QualiTrack.Api.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class RoleSeedTests(PostgresFixture database)
{
    [Fact]
    public async Task Migrating_the_database_creates_the_three_roles()
    {
        await using var context = database.CreateContext();

        var roles = await context.Roles.Select(role => role.Name).OrderBy(name => name).ToListAsync();

        Assert.Equal(["Inspector", "Investigator", "QaManager"], roles);
    }
}
