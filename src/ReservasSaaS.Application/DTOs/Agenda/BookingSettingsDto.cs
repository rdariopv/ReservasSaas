using ReservasSaaS.Domain.Enums;

namespace ReservasSaaS.Application.DTOs.Agenda;

public record BookingSettingsDto(
    Guid CompanyId,
    string Strategy,
    string StrategyDescription,
    int DefaultSlotIntervalMinutes);

public record UpdateBookingSettingsCommand(
    ProfessionalAssignmentStrategy Strategy,
    int DefaultSlotIntervalMinutes);
