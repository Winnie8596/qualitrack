using QualiTrack.Application.Abstractions;
using QualiTrack.Domain;

namespace QualiTrack.Application.NonConformances;

public sealed record AssignInvestigatorCommand(Guid NcrId, Guid InvestigatorId, DateTimeOffset DueDate);

public sealed class AssignInvestigatorHandler(NcrCommandRunner runner, ICurrentUser currentUser, IUserDirectory users)
{
    public Task<Result> HandleAsync(AssignInvestigatorCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Role != Role.QaManager)
        {
            return Task.FromResult<Result>(Error.Forbidden("Only a QA manager can assign an investigator."));
        }

        return runner.RunAsync(
            command.NcrId,
            async (ncr, stamp) =>
            {
                var investigator = new UserId(command.InvestigatorId);
                if (await users.FindRoleAsync(investigator, cancellationToken) != Role.Investigator)
                {
                    return Error.Validation($"User {command.InvestigatorId} is not an investigator.");
                }

                ncr.AssignInvestigator(investigator, command.DueDate, stamp);
                return Result.Success();
            },
            cancellationToken);
    }
}
