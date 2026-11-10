namespace QualiTrack.Application.Abstractions;

/// <summary>Commits every change a handler made as one transaction.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
