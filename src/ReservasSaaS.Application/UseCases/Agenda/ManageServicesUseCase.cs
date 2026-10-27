using Microsoft.Extensions.Logging;
using ReservasSaaS.Application.DTOs.Agenda;
using ReservasSaaS.Application.Ports.Incoming;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Domain.Security;
using ReservasSaaS.Domain.ValueObjects;

namespace ReservasSaaS.Application.UseCases.Agenda;

public class ManageServicesUseCase : IManageServicesUseCase
{
    private readonly IAuthorizeCompanyAccessUseCase _authorizer;
    private readonly IServiceRepository _serviceRepository;
    private readonly ILogger<ManageServicesUseCase> _logger;

    public ManageServicesUseCase(
        IAuthorizeCompanyAccessUseCase authorizer,
        IServiceRepository serviceRepository,
        ILogger<ManageServicesUseCase> logger)
    {
        _authorizer = authorizer;
        _serviceRepository = serviceRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ServiceDto>> GetCompanyServicesAsync(Guid companyId, bool onlyActive = false, CancellationToken cancellationToken = default)
    {
        // Public or authenticated read
        var services = onlyActive
            ? await _serviceRepository.GetActiveByCompanyIdAsync(companyId, cancellationToken)
            : await _serviceRepository.GetByCompanyIdAsync(companyId, cancellationToken);

        return services.Select(MapToDto).ToList();
    }

    public async Task<ServiceDto> GetServiceByIdAsync(Guid companyId, Guid serviceId, CancellationToken cancellationToken = default)
    {
        var service = await _serviceRepository.GetByIdAsync(serviceId, cancellationToken);
        if (service == null || service.CompanyId != companyId)
        {
            throw new EntityNotFoundException("Service", serviceId);
        }

        return MapToDto(service);
    }

    public async Task<ServiceDto> CreateServiceAsync(Guid companyId, CreateServiceCommand command, CancellationToken cancellationToken = default)
    {
        // Requires ServicesManage permission
        await _authorizer.AuthorizeAsync(companyId, Permissions.ServicesManage, cancellationToken);

        var depositPolicy = command.RequiresDeposit
            ? new DepositPolicy(true, command.DepositType, command.DepositValue, command.DepositDescription)
            : DepositPolicy.None;

        var service = new Service(
            id: Guid.NewGuid(),
            companyId: companyId,
            name: command.Name,
            description: command.Description,
            durationMinutes: command.DurationMinutes,
            price: command.Price,
            currency: command.Currency,
            bufferBeforeMinutes: command.BufferBeforeMinutes,
            bufferAfterMinutes: command.BufferAfterMinutes,
            depositPolicy: depositPolicy
        );

        await _serviceRepository.AddAsync(service, cancellationToken);
        _logger.LogInformation("Created service '{ServiceName}' ({ServiceId}) in company '{CompanyId}'",
            service.Name, service.Id, companyId);

        return MapToDto(service);
    }

    public async Task<ServiceDto> UpdateServiceAsync(Guid companyId, Guid serviceId, UpdateServiceCommand command, CancellationToken cancellationToken = default)
    {
        await _authorizer.AuthorizeAsync(companyId, Permissions.ServicesManage, cancellationToken);

        var service = await _serviceRepository.GetByIdAsync(serviceId, cancellationToken);
        if (service == null || service.CompanyId != companyId)
        {
            throw new EntityNotFoundException("Service", serviceId);
        }

        var depositPolicy = command.RequiresDeposit
            ? new DepositPolicy(true, command.DepositType, command.DepositValue, command.DepositDescription)
            : DepositPolicy.None;

        service.UpdateDetails(
            name: command.Name,
            description: command.Description,
            durationMinutes: command.DurationMinutes,
            price: command.Price,
            currency: command.Currency,
            bufferBeforeMinutes: command.BufferBeforeMinutes,
            bufferAfterMinutes: command.BufferAfterMinutes,
            depositPolicy: depositPolicy
        );

        if (service.Status != command.Status)
        {
            service.ChangeStatus(command.Status);
        }

        await _serviceRepository.UpdateAsync(service, cancellationToken);
        _logger.LogInformation("Updated service '{ServiceId}' in company '{CompanyId}'", serviceId, companyId);

        return MapToDto(service);
    }

    public async Task DeleteServiceAsync(Guid companyId, Guid serviceId, CancellationToken cancellationToken = default)
    {
        await _authorizer.AuthorizeAsync(companyId, Permissions.ServicesManage, cancellationToken);

        var service = await _serviceRepository.GetByIdAsync(serviceId, cancellationToken);
        if (service == null || service.CompanyId != companyId)
        {
            throw new EntityNotFoundException("Service", serviceId);
        }

        // Soft delete / archive
        service.ChangeStatus(ServiceStatus.Archived);
        await _serviceRepository.UpdateAsync(service, cancellationToken);

        _logger.LogInformation("Archived service '{ServiceId}' in company '{CompanyId}'", serviceId, companyId);
    }

    private static ServiceDto MapToDto(Service s) => new(
        Id: s.Id,
        CompanyId: s.CompanyId,
        Name: s.Name,
        Description: s.Description,
        DurationMinutes: s.DurationMinutes,
        BufferBeforeMinutes: s.BufferBeforeMinutes,
        BufferAfterMinutes: s.BufferAfterMinutes,
        TotalOccupiedMinutes: s.TotalOccupiedMinutes,
        Price: s.Price,
        Currency: s.Currency,
        Status: s.Status,
        DepositPolicy: new DepositPolicyDto(
            RequiresDeposit: s.DepositPolicy.RequiresDeposit,
            Type: s.DepositPolicy.Type.ToString(),
            Value: s.DepositPolicy.Value,
            Description: s.DepositPolicy.Description
        ),
        CreatedAtUtc: s.CreatedAtUtc
    );
}
