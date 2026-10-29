using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Domain.Tests;

public sealed class CorrectiveActionTests
{
    private static readonly UserId _investigator = new(Guid.NewGuid());
    private static readonly AuditStamp _added = new(_investigator, new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero));
    private static readonly AuditStamp _finished = new(_investigator, new DateTimeOffset(2026, 10, 3, 15, 30, 0, TimeSpan.Zero));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Corrective_action_description_is_required(string description)
    {
        var exception = Record.Exception(() => new CorrectiveAction(description, _added));

        Assert.IsType<DomainException>(exception);
    }

    [Fact]
    public void A_new_corrective_action_is_not_completed()
    {
        var action = new CorrectiveAction("Grind out and re-weld", _added);

        Assert.False(action.IsCompleted);
        Assert.Null(action.Completed);
    }

    [Fact]
    public void Completing_an_action_records_who_and_when()
    {
        var action = new CorrectiveAction("Grind out and re-weld", _added);

        action.Complete(_finished);

        Assert.True(action.IsCompleted);
        Assert.Equal(_finished, action.Completed);
    }

    [Fact]
    public void A_completed_action_cannot_be_completed_again()
    {
        var action = new CorrectiveAction("Grind out and re-weld", _added);
        action.Complete(_finished);
        var later = new AuditStamp(_investigator, _finished.At.AddDays(1));

        var exception = Record.Exception(() => action.Complete(later));

        Assert.IsType<DomainException>(exception);
        Assert.Equal(_finished, action.Completed);
    }
}
