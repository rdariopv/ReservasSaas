using ReservasSaaS.Domain.Entities;

namespace ReservasSaaS.Domain.Ports.Outgoing;

public interface ICompanyRepository : IRepository<Company, Guid>
{
    Task<Company?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
