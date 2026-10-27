using ReservasSaaS.Domain.Entities;

namespace ReservasSaaS.Domain.Ports.Outgoing;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByFirebaseUidAsync(string firebaseUid, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
