using QualiTrack.Application.Abstractions;

namespace QualiTrack.Application.NonConformances;

public sealed record CompleteCorrectiveActionCommand(Guid NcrId, Guid ActionId);

public sealed class CompleteCorrectiveActionHandler(NcrCommandRunner runner, ICurrentUser currentUser)
{
    public Task<Result> HandleAsync(CompleteCorrectiveActionCommand command, CancellationToken cancellationToken)
    {
        // Any investigator may call; the domain checks it is the one assigned to this NCR (BR-3)
        if (currentUser.Role != Role.Investigator)
        {
            return Task.FromResult<Result>(Error.Forbidden("Only an investigator can complete a corrective action."));
        }

        return runner.RunAsync(
            command.NcrId,
            (ncr, stamp) => ncr.CompleteAction(command.ActionId, stamp),
            cancellationToken);
    }
}
