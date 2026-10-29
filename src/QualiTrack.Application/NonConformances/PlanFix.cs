using QualiTrack.Application.Abstractions;
using QualiTrack.Domain;

namespace QualiTrack.Application.NonConformances;

public sealed record PlanFixCommand(Guid NcrId, string RootCause, IReadOnlyList<string> ActionDescriptions);

public sealed class PlanFixHandler(
    INcrRepository repository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser)
{
    public async Task<Result> HandleAsync(PlanFixCommand command, CancellationToken cancellationToken)
    {
        // Any investigator may call; the domain checks it is the one assigned to this NCR (BR-3)
        if (currentUser.Role != Role.Investigator)
        {
            return Error.Forbidden("Only an investigator can plan a fix.");
        }

        var ncr = await repository.GetByIdAsync(command.NcrId, cancellationToken);
        if (ncr is null)
        {
            return Error.NotFound($"NCR {command.NcrId} was not found.");
        }

        try
        {
            ncr.PlanFix(command.RootCause, command.ActionDescriptions, new AuditStamp(currentUser.Id, clock.UtcNow));
        }
        catch (DomainException exception)
        {
            return DomainErrors.From(exception);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
