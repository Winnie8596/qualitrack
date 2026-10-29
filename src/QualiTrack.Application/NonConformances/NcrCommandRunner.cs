using QualiTrack.Application.Abstractions;
using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.NonConformances;

/// <summary>
/// The steps every command on an existing NCR shares: load it, stamp the change with who and when,
/// turn a broken rule into an error, and save only when the change succeeded.
/// </summary>
public sealed class NcrCommandRunner(INcrRepository repository, IUnitOfWork unitOfWork, IClock clock, ICurrentUser currentUser)
{
    public Task<Result> RunAsync(Guid ncrId, Action<NonConformance, AuditStamp> change, CancellationToken cancellationToken) =>
        RunAsync(
            ncrId,
            (ncr, stamp) =>
            {
                change(ncr, stamp);
                return Task.FromResult(Result.Success());
            },
            cancellationToken);

    // The change may return a failed result of its own, e.g. after checking something the domain cannot
    public async Task<Result> RunAsync(
        Guid ncrId,
        Func<NonConformance, AuditStamp, Task<Result>> change,
        CancellationToken cancellationToken)
    {
        var ncr = await repository.GetByIdAsync(ncrId, cancellationToken);
        if (ncr is null)
        {
            return Error.NotFound($"NCR {ncrId} was not found.");
        }

        Result result;
        try
        {
            result = await change(ncr, new AuditStamp(currentUser.Id, clock.UtcNow));
        }
        catch (DomainException exception)
        {
            return DomainErrors.From(exception);
        }

        if (result.IsSuccess)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return result;
    }
}
