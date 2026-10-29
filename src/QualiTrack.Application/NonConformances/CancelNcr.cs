using QualiTrack.Application.Abstractions;

namespace QualiTrack.Application.NonConformances;

public sealed record CancelNcrCommand(Guid NcrId, string Reason);

public sealed class CancelNcrHandler(NcrCommandRunner runner, ICurrentUser currentUser)
{
    public Task<Result> HandleAsync(CancelNcrCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Role != Role.QaManager)
        {
            return Task.FromResult<Result>(Error.Forbidden("Only a QA manager can cancel an NCR."));
        }

        return runner.RunAsync(command.NcrId, (ncr, stamp) => ncr.Cancel(command.Reason, stamp), cancellationToken);
    }
}
