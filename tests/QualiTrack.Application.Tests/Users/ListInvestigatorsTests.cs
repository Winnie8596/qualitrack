using QualiTrack.Application.Abstractions;
using QualiTrack.Application.Tests.Fakes;
using QualiTrack.Application.Users;

namespace QualiTrack.Application.Tests.Users;

public sealed class ListInvestigatorsTests
{
    private readonly InMemoryUserDirectory _users = new();

    [Fact]
    public async Task A_qa_manager_gets_every_investigator_by_email_to_choose_an_assignee_from()
    {
        var bea = _users.Add(Role.Investigator, "bea@example.com");
        var al = _users.Add(Role.Investigator, "al@example.com");
        _users.Add(Role.Inspector, "ivy@example.com");

        var result = await ListAsync(Role.QaManager);

        Assert.Equal([new UserView(al.Value, "al@example.com"), new UserView(bea.Value, "bea@example.com")], result.Value);
    }

    [Theory]
    [InlineData(Role.Inspector)]
    [InlineData(Role.Investigator)]
    public async Task Only_a_qa_manager_may_list_investigators(Role role)
    {
        var result = await ListAsync(role);

        Assert.Equal(ErrorKind.Forbidden, result.Error.Kind);
    }

    private Task<Result<IReadOnlyList<UserView>>> ListAsync(Role role) =>
        new ListInvestigatorsHandler(_users, new FakeCurrentUser(role)).HandleAsync(CancellationToken.None);
}
