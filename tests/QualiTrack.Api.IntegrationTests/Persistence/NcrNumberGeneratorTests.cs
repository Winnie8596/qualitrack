using QualiTrack.Domain.NonConformances;
using QualiTrack.Infrastructure.Persistence;

namespace QualiTrack.Api.IntegrationTests.Persistence;

// The database is shared with other persistence tests, so each test uses a year of its own
[Collection(PostgresCollection.Name)]
public sealed class NcrNumberGeneratorTests(PostgresFixture database)
{
    [Fact]
    public async Task Numbers_in_a_year_start_at_one_and_follow_on()
    {
        await using var context = database.CreateContext();
        var generator = new NcrNumberGenerator(context);

        var first = await generator.NextAsync(2101, CancellationToken.None);
        var second = await generator.NextAsync(2101, CancellationToken.None);

        Assert.Equal(NcrNumber.From(2101, 1), first);
        Assert.Equal(NcrNumber.From(2101, 2), second);
    }

    [Fact]
    public async Task Each_year_has_its_own_sequence()
    {
        await using var context = database.CreateContext();
        var generator = new NcrNumberGenerator(context);
        await generator.NextAsync(2102, CancellationToken.None);

        var firstOfNextYear = await generator.NextAsync(2103, CancellationToken.None);

        Assert.Equal(NcrNumber.From(2103, 1), firstOfNextYear);
    }

    [Fact]
    public async Task Concurrent_raises_get_different_numbers()
    {
        const int Raises = 20;

        var numbers = await Task.WhenAll(Enumerable.Range(0, Raises).Select(_ => NextInOwnContextAsync(2104)));

        Assert.Equal(
            Enumerable.Range(1, Raises).Select(sequence => NcrNumber.From(2104, sequence)),
            numbers.OrderBy(number => number.Value));
    }

    // One context per caller, as in production where each request has its own
    private async Task<NcrNumber> NextInOwnContextAsync(int year)
    {
        await using var context = database.CreateContext();
        return await new NcrNumberGenerator(context).NextAsync(year, CancellationToken.None);
    }
}
