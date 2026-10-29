using QualiTrack.Application.Abstractions;
using QualiTrack.Domain;

namespace QualiTrack.Application.NonConformances;

public sealed record AssignInvestigatorCommand(Guid NcrId, Guid InvestigatorId, DateTimeOffset DueDate);

public sealed class AssignInvestigatorHandler(
    INcrRepository repository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IUserDirectory users)
{
    public async Task<Result> HandleAsync(AssignInvestigatorCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Role != Role.QaManager)
        {
            return Error.Forbidden("Only a QA manager can assign an investigator.");
        }

        var ncr = await repository.GetByIdAsync(command.NcrId, cancellationToken);
        if (ncr is null)
        {
            return Error.NotFound($"NCR {command.NcrId} was not found.");
        }

        try
        {
            var investigator = new UserId(command.InvestigatorId);
            if (await users.FindRoleAsync(investigator, cancellationToken) != Role.Investigator)
            {
                return Error.Validation($"User {command.InvestigatorId} is not an investigator.");
            }

            ncr.AssignInvestigator(investigator, command.DueDate, new AuditStamp(currentUser.Id, clock.UtcNow));
        }
        catch (DomainException exception)
        {
            return DomainErrors.From(exception);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
