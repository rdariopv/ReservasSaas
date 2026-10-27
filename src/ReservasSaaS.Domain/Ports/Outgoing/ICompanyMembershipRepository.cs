using ReservasSaaS.Domain.Entities;

namespace ReservasSaaS.Domain.Ports.Outgoing;

public interface ICompanyMembershipRepository : IRepository<CompanyMembership, Guid>
{
    Task<CompanyMembership?> GetMembershipAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompanyMembership>> GetUserMembershipsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompanyMembership>> GetCompanyMembersAsync(Guid companyId, CancellationToken cancellationToken = default);
}
