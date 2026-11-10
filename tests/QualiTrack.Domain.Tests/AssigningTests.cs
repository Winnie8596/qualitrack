using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Domain.Tests;

public sealed class AssigningTests
{
    [Fact]
    public void Assigning_an_investigator_starts_the_investigation()
    {
        var ncr = Ncrs.Open();
        var dueDate = Ncrs.RaisedAt.AddDays(14);

        ncr.AssignInvestigator(Ncrs.Investigator, dueDate, Ncrs.Day(1, Ncrs.Manager));

        Assert.Equal(NcrStatus.Investigating, ncr.Status);
        Assert.Equal(Ncrs.Investigator, ncr.InvestigatorId);
        Assert.Equal(dueDate, ncr.DueDate);
    }

    [Fact]
    public void Critical_ncr_cannot_be_due_more_than_7_days_after_it_was_raised()
    {
        var ncr = Ncrs.Open(Severity.Critical);
        var tooLate = Ncrs.RaisedAt.AddDays(7).AddMinutes(1);

        var exception = Record.Exception(() => ncr.AssignInvestigator(Ncrs.Investigator, tooLate, Ncrs.Day(1, Ncrs.Manager)));

        Assert.Equal(DomainErrorKind.InvalidInput, Assert.IsType<DomainException>(exception).Kind);
    }

    [Fact]
    public void Critical_ncr_can_be_due_on_day_7()
    {
        var ncr = Ncrs.Open(Severity.Critical);
        var daySeven = Ncrs.RaisedAt.AddDays(7);

        ncr.AssignInvestigator(Ncrs.Investigator, daySeven, Ncrs.Day(1, Ncrs.Manager));

        Assert.Equal(daySeven, ncr.DueDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Due_date_must_be_in_the_future(int daysFromAssignment)
    {
        var ncr = Ncrs.Open();
        var assigned = Ncrs.Day(2, Ncrs.Manager);
        var dueDate = assigned.At.AddDays(daysFromAssignment);

        var exception = Record.Exception(() => ncr.AssignInvestigator(Ncrs.Investigator, dueDate, assigned));

        Assert.Equal(DomainErrorKind.InvalidInput, Assert.IsType<DomainException>(exception).Kind);
    }

    [Fact]
    public void Due_date_must_be_utc()
    {
        var ncr = Ncrs.Open();
        var dueInKualaLumpurTime = Ncrs.RaisedAt.AddDays(5).ToOffset(TimeSpan.FromHours(8));

        var exception = Record.Exception(() => ncr.AssignInvestigator(Ncrs.Investigator, dueInKualaLumpurTime, Ncrs.Day(1, Ncrs.Manager)));

        Assert.Equal(DomainErrorKind.InvalidInput, Assert.IsType<DomainException>(exception).Kind);
    }
}
