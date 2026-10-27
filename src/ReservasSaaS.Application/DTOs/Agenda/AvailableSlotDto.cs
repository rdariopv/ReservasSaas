namespace ReservasSaaS.Application.DTOs.Agenda;

public record AvailableProfessionalDto(Guid Id, string Name);

public record AvailableSlotDto(
    DateTime StartUtc,
    DateTime EndUtc,
    string StartTimeLocal,
    string EndTimeLocal,
    Guid ServiceId,
    string ServiceName,
    int DurationMinutes,
    decimal Price,
    string Currency,
    bool RequiresDeposit,
    decimal DepositAmount,
    string DepositDescription,
    Guid? AssignedProfessionalId,
    string? AssignedProfessionalName,
    IReadOnlyList<AvailableProfessionalDto> AvailableProfessionals);

public record DayAvailabilityDto(
    DateOnly Date,
    Guid CompanyId,
    string CompanyName,
    string TimeZone,
    string AssignmentStrategy,
    int TotalSlotsAvailable,
    IReadOnlyList<AvailableSlotDto> Slots);
