namespace QualiTrack.Domain.Tests;

public sealed class UserIdTests
{
    [Fact]
    public void User_id_cannot_be_empty()
    {
        var empty = Guid.Empty;

        var exception = Record.Exception(() => new UserId(empty));

        Assert.IsType<DomainException>(exception);
    }
}
