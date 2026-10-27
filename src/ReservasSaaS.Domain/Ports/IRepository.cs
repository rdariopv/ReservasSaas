using ReservasSaaS.Domain.Common;

namespace ReservasSaaS.Domain.Ports;

/// <summary>
/// Generic repository port defining persistence contracts according to Hexagonal Architecture.
/// Pure domain abstraction: independent of Firestore, SQL or any specific database engine.
/// </summary>
public interface IRepository<TEntity, TId> where TEntity : BaseEntity<TId>, IAggregateRoot
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TId id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
}
