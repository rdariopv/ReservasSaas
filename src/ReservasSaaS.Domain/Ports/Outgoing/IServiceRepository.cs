using ReservasSaaS.Domain.Entities;

namespace ReservasSaaS.Domain.Ports.Outgoing;

public interface IServiceRepository : IRepository<Service, Guid>
{
    Task<IReadOnlyList<Service>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Service>> GetActiveByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Service?> GetByIdAsync(string companyId,string serviceId,CancellationToken cancellationToken = default);
}
