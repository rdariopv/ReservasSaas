using ReservasSaaS.Domain.Entities;

namespace ReservasSaaS.Domain.Ports.Outgoing;

public interface IProfessionalRepository : IRepository<Professional, Guid>
{
    Task<IReadOnlyList<Professional>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Professional>> GetByServiceIdAsync(Guid companyId, Guid serviceId, CancellationToken cancellationToken = default);
    Task<Professional?> GetByUserIdAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default);


}
