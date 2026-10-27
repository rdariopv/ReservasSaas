using ReservasSaaS.Application.DTOs.Agenda;

namespace ReservasSaaS.Application.Ports.Incoming;

public interface IManageServicesUseCase
{
    Task<IReadOnlyList<ServiceDto>> GetCompanyServicesAsync(Guid companyId, bool onlyActive = false, CancellationToken cancellationToken = default);
    Task<ServiceDto> GetServiceByIdAsync(Guid companyId, Guid serviceId, CancellationToken cancellationToken = default);
    Task<ServiceDto> CreateServiceAsync(Guid companyId, CreateServiceCommand command, CancellationToken cancellationToken = default);
    Task<ServiceDto> UpdateServiceAsync(Guid companyId, Guid serviceId, UpdateServiceCommand command, CancellationToken cancellationToken = default);
    Task DeleteServiceAsync(Guid companyId, Guid serviceId, CancellationToken cancellationToken = default);
}
