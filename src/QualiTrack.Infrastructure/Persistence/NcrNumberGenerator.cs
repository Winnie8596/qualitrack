using Microsoft.EntityFrameworkCore;
using QualiTrack.Application.Abstractions;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Infrastructure.Persistence;

public sealed class NcrNumberGenerator(QualiTrackDbContext context) : INcrNumberGenerator
{
    // One statement, so PostgreSQL locks the year's row while it adds one: concurrent callers queue on that lock
    // and each gets its own number. Reading the last number and writing it back in two steps would let two
    // callers read the same value. The first number of a year inserts the row instead.
    private const string NextSequenceSql =
        $$"""
        INSERT INTO "{{NcrSequence.Table}}" ("{{NcrSequence.YearColumn}}", "{{NcrSequence.LastSequenceColumn}}")
        VALUES ({0}, 1)
        ON CONFLICT ("{{NcrSequence.YearColumn}}")
        DO UPDATE SET "{{NcrSequence.LastSequenceColumn}}" = "{{NcrSequence.Table}}"."{{NcrSequence.LastSequenceColumn}}" + 1
        RETURNING "{{NcrSequence.LastSequenceColumn}}" AS "Value"
        """;

    public async Task<NcrNumber> NextAsync(int year, CancellationToken cancellationToken)
    {
        var sequences = await context.Database.SqlQueryRaw<int>(NextSequenceSql, year).ToListAsync(cancellationToken);
        return NcrNumber.From(year, sequences.Single());
    }
}
