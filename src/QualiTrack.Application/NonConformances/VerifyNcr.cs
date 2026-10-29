using QualiTrack.Application.Abstractions;

namespace QualiTrack.Application.NonConformances;

public sealed record VerifyNcrCommand(Guid NcrId);

public sealed class VerifyNcrHandler(NcrCommandRunner runner, ICurrentUser currentUser)
{
    public Task<Result> HandleAsync(VerifyNcrCommand command, CancellationToken cancellationToken)
    {
        // The domain still checks the verifier is not the investigator (BR-5)
        if (currentUser.Role != Role.QaManager)
        {
            return Task.FromResult<Result>(Error.Forbidden("Only a QA manager can verify a fix."));
        }

        return runner.RunAsync(command.NcrId, (ncr, stamp) => ncr.Verify(stamp), cancellationToken);
    }
}
