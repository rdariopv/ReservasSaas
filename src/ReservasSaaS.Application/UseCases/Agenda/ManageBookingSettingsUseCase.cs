using Microsoft.Extensions.Logging;
using ReservasSaaS.Application.DTOs.Agenda;
using ReservasSaaS.Application.Ports.Incoming;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Domain.Security;

namespace ReservasSaaS.Application.UseCases.Agenda;

public class ManageBookingSettingsUseCase : IManageBookingSettingsUseCase
{
    private readonly IAuthorizeCompanyAccessUseCase _authorizer;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<ManageBookingSettingsUseCase> _logger;

    public ManageBookingSettingsUseCase(
        IAuthorizeCompanyAccessUseCase authorizer,
        ICompanyRepository companyRepository,
        ILogger<ManageBookingSettingsUseCase> logger)
    {
        _authorizer = authorizer;
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<BookingSettingsDto> GetSettingsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);
        if (company == null)
        {
            throw new EntityNotFoundException("Company", companyId);
        }

        return MapToDto(company);
    }

    public async Task<BookingSettingsDto> UpdateSettingsAsync(
        Guid companyId, 
        UpdateBookingSettingsCommand command, 
        CancellationToken cancellationToken = default)
    {
        await _authorizer.AuthorizeAsync(companyId, Permissions.CompanySettings, cancellationToken);

        var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);
        if (company == null)
        {
            throw new EntityNotFoundException("Company", companyId);
        }

        company.SetBookingSettings(command.Strategy, command.DefaultSlotIntervalMinutes);
        await _companyRepository.UpdateAsync(company, cancellationToken);

        _logger.LogInformation("Updated booking assignment strategy to '{Strategy}' for company '{CompanyId}'",
            command.Strategy, companyId);

        return MapToDto(company);
    }

    private static BookingSettingsDto MapToDto(Company c) => new(
        CompanyId: c.Id,
        Strategy: c.AssignmentStrategy.ToString(),
        StrategyDescription: c.AssignmentStrategy == ProfessionalAssignmentStrategy.ClientSelectsProfessional
            ? "1. El cliente selecciona profesional de su preferencia."
            : "2. El negocio asigna al profesional después de recibir la solicitud o por disponibilidad.",
        DefaultSlotIntervalMinutes: c.DefaultSlotIntervalMinutes
    );
}
