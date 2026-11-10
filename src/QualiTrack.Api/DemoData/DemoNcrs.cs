using Microsoft.AspNetCore.Identity;
using QualiTrack.Api.Authentication;
using QualiTrack.Application.Abstractions;
using QualiTrack.Domain;

namespace QualiTrack.Api.DemoData;

/// <summary>
/// Fills an empty database with the demo's NCRs, so the list, the overdue badge and the dashboard have something
/// to show. Only where DemoData:Seed is on, after the demo users exist; see Program.
/// </summary>
public static class DemoNcrs
{
    // Long enough ago for the newest NCR to have been triaged already
    private static readonly TimeSpan _ageOfNewest = TimeSpan.FromHours(6);

    public static async Task SeedDemoNcrsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<INcrRepository>();

        // Any NCR at all means the demo has been seeded or used: its data is left alone
        var anyNcr = new NcrListCriteria([], NcrSortField.RaisedAt, SortDirection.Descending, Skip: 0, Take: 1);
        if ((await repository.ListAsync(anyNcr, cancellationToken)).Count > 0)
        {
            return;
        }

        var builder = new DemoNcrBuilder(
            await DemoCastAsync(scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>()));
        var numbers = scope.ServiceProvider.GetRequiredService<INcrNumberGenerator>();
        var now = scope.ServiceProvider.GetRequiredService<IClock>().UtcNow;
        var catalogue = DemoNcrCatalogue.All;

        for (var index = 0; index < catalogue.Count; index++)
        {
            var raisedAt = now - _ageOfNewest - TimeSpan.FromDays(catalogue.Count - 1 - index);
            var number = await numbers.NextAsync(raisedAt.Year, cancellationToken);
            repository.Add(builder.Build(catalogue[index], number, raisedAt));
        }

        // One transaction: the demo gets every NCR or none
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
    }

    private static async Task<DemoCast> DemoCastAsync(UserManager<IdentityUser<Guid>> users) =>
        new(
            await DemoUserIdAsync(users, Role.Inspector),
            await DemoUserIdAsync(users, Role.Investigator),
            await DemoUserIdAsync(users, Role.QaManager));

    private static async Task<UserId> DemoUserIdAsync(UserManager<IdentityUser<Guid>> users, Role role)
    {
        var email = DemoUsers.EmailOf(role);
        var user = await users.FindByEmailAsync(email)
            ?? throw new InvalidOperationException($"Demo user '{email}' must exist before the demo NCRs are seeded.");
        return new UserId(user.Id);
    }
}
