using QualiTrack.Application.Abstractions;
using QualiTrack.Infrastructure.Identity;

namespace QualiTrack.Api.IntegrationTests.Authentication;

public sealed class IdentityRolesTests
{
    [Fact]
    public void Each_role_is_read_back_from_its_name() =>
        Assert.All(Enum.GetValues<Role>(), role => Assert.Equal(role, IdentityRoles.From([role.ToString()])));

    [Theory]
    [InlineData("1")]
    [InlineData("inspector")]
    [InlineData("Auditor")]
    [InlineData(null)]
    public void A_name_that_is_not_exactly_a_role_is_no_role(string? name) => Assert.Null(IdentityRoles.From([name]));

    [Fact]
    public void Other_names_are_ignored_beside_the_one_role() =>
        Assert.Equal(Role.Investigator, IdentityRoles.From(["Auditor", "Investigator"]));

    [Fact]
    public void A_user_with_two_roles_breaks_the_one_role_rule() =>
        Assert.Throws<InvalidOperationException>(() => IdentityRoles.From(["Inspector", "QaManager"]));
}
