using QualiTrack.Application.Abstractions;

namespace QualiTrack.Application.NonConformances;

public sealed record AddCorrectiveActionCommand(Guid NcrId, string Description);

public sealed class AddCorrectiveActionHandler(NcrCommandRunner runner, ICurrentUser currentUser)
{
    public Task<Result> HandleAsync(AddCorrectiveActionCommand command, CancellationToken cancellationToken)
    {
        // Any investigator may call; the domain checks it is the one assigned to this NCR (BR-3)
        if (currentUser.Role != Role.Investigator)
        {
            return Task.FromResult<Result>(Error.Forbidden("Only an investigator can add a corrective action."));
        }

        return runner.RunAsync(
            command.NcrId,
            (ncr, stamp) => ncr.AddCorrectiveAction(command.Description, stamp),
            cancellationToken);
    }
}
