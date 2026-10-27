using ReservasSaaS.Application.DTOs.Identity;

namespace ReservasSaaS.Application.Ports.Incoming;

public interface IGetUserCompaniesUseCase
{
    Task<IReadOnlyList<UserCompanyAccessDto>> ExecuteAsync(CancellationToken cancellationToken = default);
}
