using ReservasSaaS.Application.DTOs.Identity;

namespace ReservasSaaS.Application.Ports.Incoming;

public interface IGetCompanyMembersUseCase
{
    Task<IReadOnlyList<CompanyMemberDto>> ExecuteAsync(Guid companyId, CancellationToken cancellationToken = default);
}
