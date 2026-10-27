using Microsoft.Extensions.Logging;
using ReservasSaaS.Application.DTOs.Agenda;
using ReservasSaaS.Application.Ports.Incoming;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Domain.Security;
using ReservasSaaS.Domain.ValueObjects;

namespace ReservasSaaS.Application.UseCases.Agenda;

public class ManageProfessionalsUseCase : IManageProfessionalsUseCase
{
    private readonly IAuthorizeCompanyAccessUseCase _authorizer;
    private readonly IProfessionalRepository _professionalRepository;
    private readonly ILogger<ManageProfessionalsUseCase> _logger;

    public ManageProfessionalsUseCase(
        IAuthorizeCompanyAccessUseCase authorizer,
        IProfessionalRepository professionalRepository,
        ILogger<ManageProfessionalsUseCase> logger)
    {
        _authorizer = authorizer;
        _professionalRepository = professionalRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProfessionalDto>> GetCompanyProfessionalsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var professionals = await _professionalRepository.GetByCompanyIdAsync(companyId, cancellationToken);
        return professionals.Select(MapToDto).ToList();
    }

    public async Task<ProfessionalDto> GetProfessionalByIdAsync(Guid companyId, Guid professionalId, CancellationToken cancellationToken = default)
    {
        var professional = await _professionalRepository.GetByIdAsync(professionalId, cancellationToken);
        if (professional == null || professional.CompanyId != companyId)
        {
            throw new EntityNotFoundException("Professional", professionalId);
        }

        return MapToDto(professional);
    }

    public async Task<ProfessionalDto> CreateProfessionalAsync(Guid companyId, CreateProfessionalCommand command, CancellationToken cancellationToken = default)
    {
        await _authorizer.AuthorizeAsync(companyId, Permissions.MembersInvite, cancellationToken);

        var professional = new Professional(
            id: Guid.NewGuid(),
            companyId: companyId,
            name: command.Name,
            userId: command.UserId,
            email: command.Email,
            phoneNumber: command.PhoneNumber,
            specialty: command.Specialty,
            assignedServiceIds: command.AssignedServiceIds
        );

        await _professionalRepository.AddAsync(professional, cancellationToken);
        _logger.LogInformation("Created professional '{ProfessionalName}' ({ProfessionalId}) in company '{CompanyId}'",
            professional.Name, professional.Id, companyId);

        return MapToDto(professional);
    }

    public async Task<ProfessionalDto> UpdateScheduleAsync(
        Guid companyId, 
        Guid professionalId, 
        UpdateProfessionalScheduleCommand command, 
        CancellationToken cancellationToken = default)
    {
        await _authorizer.AuthorizeAsync(companyId, Permissions.AppointmentsManage, cancellationToken);

        var professional = await _professionalRepository.GetByIdAsync(professionalId, cancellationToken);
        if (professional == null || professional.CompanyId != companyId)
        {
            throw new EntityNotFoundException("Professional", professionalId);
        }

        var workingSchedules = command.WorkingSchedules.Select(w =>
        {
            var day = Enum.Parse<DayOfWeek>(w.DayOfWeek, ignoreCase: true);
            var start = TimeOnly.Parse(w.StartTime);
            var end = TimeOnly.Parse(w.EndTime);
            return new WorkingSchedule(day, start, end, w.IsEnabled);
        });

        var breakSchedules = command.BreakSchedules?.Select(b =>
        {
            var day = Enum.Parse<DayOfWeek>(b.DayOfWeek, ignoreCase: true);
            var start = TimeOnly.Parse(b.StartTime);
            var end = TimeOnly.Parse(b.EndTime);
            return new BreakSchedule(day, start, end, b.Name);
        });

        var exceptions = command.Exceptions?.Select(e =>
        {
            var date = DateOnly.Parse(e.Date);
            TimeOnly? start = e.StartTime != null ? TimeOnly.Parse(e.StartTime) : null;
            TimeOnly? end = e.EndTime != null ? TimeOnly.Parse(e.EndTime) : null;
            return new ScheduleException(date, e.IsDayOff, start, end, e.Reason);
        });

        professional.SetSchedule(workingSchedules, breakSchedules, exceptions);
        await _professionalRepository.UpdateAsync(professional, cancellationToken);

        _logger.LogInformation("Updated working schedules and exceptions for professional '{ProfessionalId}'", professionalId);

        return MapToDto(professional);
    }

    private static ProfessionalDto MapToDto(Professional p) => new(
        Id: p.Id,
        CompanyId: p.CompanyId,
        UserId: p.UserId,
        Name: p.Name,
        Email: p.Email,
        PhoneNumber: p.PhoneNumber,
        Specialty: p.Specialty,
        IsActive: p.IsActive,
        AssignedServiceIds: p.AssignedServiceIds,
        WorkingSchedules: p.WorkingSchedules.Select(w => new WorkingScheduleDto(
            DayOfWeek: w.DayOfWeek.ToString(),
            StartTime: w.StartTime.ToString("HH:mm"),
            EndTime: w.EndTime.ToString("HH:mm"),
            IsEnabled: w.IsEnabled
        )).ToList(),
        BreakSchedules: p.BreakSchedules.Select(b => new BreakScheduleDto(
            DayOfWeek: b.DayOfWeek.ToString(),
            StartTime: b.StartTime.ToString("HH:mm"),
            EndTime: b.EndTime.ToString("HH:mm"),
            Name: b.Name
        )).ToList(),
        Exceptions: p.Exceptions.Select(e => new ScheduleExceptionDto(
            Date: e.Date.ToString("yyyy-MM-dd"),
            IsDayOff: e.IsDayOff,
            StartTime: e.StartTime?.ToString("HH:mm"),
            EndTime: e.EndTime?.ToString("HH:mm"),
            Reason: e.Reason
        )).ToList(),
        CreatedAtUtc: p.CreatedAtUtc
    );
}
