namespace ReservasSaaS.Domain.Ports;

/// <summary>
/// Unit of work contract for atomic state persistence.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
