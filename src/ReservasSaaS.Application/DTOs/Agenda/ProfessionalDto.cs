namespace ReservasSaaS.Application.DTOs.Agenda;

public record WorkingScheduleDto(
    string DayOfWeek,
    string StartTime,
    string EndTime,
    bool IsEnabled);

public record BreakScheduleDto(
    string DayOfWeek,
    string StartTime,
    string EndTime,
    string Name);

public record ScheduleExceptionDto(
    string Date,
    bool IsDayOff,
    string? StartTime,
    string? EndTime,
    string Reason);

public record ProfessionalDto(
    Guid Id,
    Guid CompanyId,
    Guid? UserId,
    string Name,
    string? Email,
    string? PhoneNumber,
    string? Specialty,
    bool IsActive,
    IReadOnlyCollection<Guid> AssignedServiceIds,
    IReadOnlyList<WorkingScheduleDto> WorkingSchedules,
    IReadOnlyList<BreakScheduleDto> BreakSchedules,
    IReadOnlyList<ScheduleExceptionDto> Exceptions,
    DateTime CreatedAtUtc);

public record CreateProfessionalCommand(
    string Name,
    Guid? UserId = null,
    string? Email = null,
    string? PhoneNumber = null,
    string? Specialty = null,
    IReadOnlyList<Guid>? AssignedServiceIds = null);

public record UpdateProfessionalScheduleCommand(
    IReadOnlyList<WorkingScheduleDto> WorkingSchedules,
    IReadOnlyList<BreakScheduleDto>? BreakSchedules = null,
    IReadOnlyList<ScheduleExceptionDto>? Exceptions = null);
