using QualiTrack.Application.Abstractions;

namespace QualiTrack.Application.Users;

public sealed record UserView(Guid Id, string Email);

/// <summary>The investigators a QA manager can assign an NCR to (A2), sorted by email for the picker.</summary>
public sealed class ListInvestigatorsHandler(IUserDirectory users, ICurrentUser currentUser)
{
    public async Task<Result<IReadOnlyList<UserView>>> HandleAsync(CancellationToken cancellationToken)
    {
        if (currentUser.Role != Role.QaManager)
        {
            return Error.Forbidden("Only a QA manager can list investigators.");
        }

        var investigators = await users.ListAsync(Role.Investigator, cancellationToken);
        return investigators
            .Select(user => new UserView(user.Id.Value, user.Email))
            .OrderBy(user => user.Email, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
