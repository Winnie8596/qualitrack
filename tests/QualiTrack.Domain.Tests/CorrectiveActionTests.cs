using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Domain.Tests;

public sealed class CorrectiveActionTests
{
    private static readonly UserId Investigator = new(Guid.NewGuid());
    private static readonly AuditStamp Added = new(Investigator, new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero));
    private static readonly AuditStamp Finished = new(Investigator, new DateTimeOffset(2026, 10, 3, 15, 30, 0, TimeSpan.Zero));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Corrective_action_description_is_required(string description)
    {
        var exception = Record.Exception(() => new CorrectiveAction(description, Added));

        Assert.IsType<DomainException>(exception);
    }

    [Fact]
    public void A_new_corrective_action_is_not_completed()
    {
        var action = new CorrectiveAction("Grind out and re-weld", Added);

        Assert.False(action.IsCompleted);
        Assert.Null(action.Completed);
    }

    [Fact]
    public void Completing_an_action_records_who_and_when()
    {
        var action = new CorrectiveAction("Grind out and re-weld", Added);

        action.Complete(Finished);

        Assert.True(action.IsCompleted);
        Assert.Equal(Finished, action.Completed);
    }

    [Fact]
    public void A_completed_action_cannot_be_completed_again()
    {
        var action = new CorrectiveAction("Grind out and re-weld", Added);
        action.Complete(Finished);
        var later = Finished with { At = Finished.At.AddDays(1) };

        var exception = Record.Exception(() => action.Complete(later));

        Assert.IsType<DomainException>(exception);
        Assert.Equal(Finished, action.Completed);
    }
}
