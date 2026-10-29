using QualiTrack.Application.Abstractions;

namespace QualiTrack.Application.NonConformances;

public sealed record PlanFixCommand(Guid NcrId, string RootCause, IReadOnlyList<string> ActionDescriptions);

public sealed class PlanFixHandler(NcrCommandRunner runner, ICurrentUser currentUser)
{
    public Task<Result> HandleAsync(PlanFixCommand command, CancellationToken cancellationToken)
    {
        // Any investigator may call; the domain checks it is the one assigned to this NCR (BR-3)
        if (currentUser.Role != Role.Investigator)
        {
            return Task.FromResult<Result>(Error.Forbidden("Only an investigator can plan a fix."));
        }

        return runner.RunAsync(
            command.NcrId,
            (ncr, stamp) => ncr.PlanFix(command.RootCause, command.ActionDescriptions, stamp),
            cancellationToken);
    }
}
