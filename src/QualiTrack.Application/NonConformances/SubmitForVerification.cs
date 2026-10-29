using QualiTrack.Application.Abstractions;

namespace QualiTrack.Application.NonConformances;

public sealed record SubmitForVerificationCommand(Guid NcrId);

public sealed class SubmitForVerificationHandler(NcrCommandRunner runner, ICurrentUser currentUser)
{
    public Task<Result> HandleAsync(SubmitForVerificationCommand command, CancellationToken cancellationToken)
    {
        // Any investigator may call; the domain checks it is the one assigned to this NCR (BR-3)
        if (currentUser.Role != Role.Investigator)
        {
            return Task.FromResult<Result>(Error.Forbidden("Only an investigator can submit for verification."));
        }

        return runner.RunAsync(command.NcrId, (ncr, stamp) => ncr.SubmitForVerification(stamp), cancellationToken);
    }
}
