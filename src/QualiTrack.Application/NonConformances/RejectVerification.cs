using QualiTrack.Application.Abstractions;

namespace QualiTrack.Application.NonConformances;

public sealed record RejectVerificationCommand(Guid NcrId, string Reason);

public sealed class RejectVerificationHandler(NcrCommandRunner runner, ICurrentUser currentUser)
{
    public Task<Result> HandleAsync(RejectVerificationCommand command, CancellationToken cancellationToken)
    {
        // The domain still checks the verifier is not the investigator (BR-5)
        if (currentUser.Role != Role.QaManager)
        {
            return Task.FromResult<Result>(Error.Forbidden("Only a QA manager can reject a fix."));
        }

        return runner.RunAsync(
            command.NcrId,
            (ncr, stamp) => ncr.RejectVerification(command.Reason, stamp),
            cancellationToken);
    }
}
