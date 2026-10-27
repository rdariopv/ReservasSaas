using ReservasSaaS.Application.DTOs.Identity;

namespace ReservasSaaS.Application.Ports.Incoming;

public interface ICreateCompanyUseCase
{
    Task<CompanyDto> ExecuteAsync(CreateCompanyCommand command, CancellationToken cancellationToken = default);
}
