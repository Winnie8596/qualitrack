using QualiTrack.Application.Abstractions;
using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.NonConformances;

public sealed record RaiseNcrCommand(string Title, string Description, string Location, Severity Severity);

public sealed class RaiseNcrHandler(
    INcrRepository repository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    INcrNumberGenerator numberGenerator)
{
    public async Task<Result<Guid>> HandleAsync(RaiseNcrCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Role != Role.Inspector)
        {
            return Error.Forbidden("Only an inspector can raise an NCR.");
        }

        // Built before taking a number so a rejected NCR does not use up a sequence value
        NcrDetails details;
        try
        {
            details = new NcrDetails(command.Title, command.Description, command.Location, command.Severity);
        }
        catch (DomainException exception)
        {
            return DomainErrors.From(exception);
        }

        var now = clock.UtcNow;
        var number = await numberGenerator.NextAsync(now.Year, cancellationToken);
        var ncr = NonConformance.Raise(number, details, new AuditStamp(currentUser.Id, now));

        repository.Add(ncr);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ncr.Id;
    }
}
