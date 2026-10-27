using ReservasSaaS.Application.DTOs.Agenda;

namespace ReservasSaaS.Application.Ports.Incoming;

/// <summary>
/// Primary port for querying available scheduling slots.
/// Centralized engine shared across Flutter, Web, and WhatsApp chatbot.
/// </summary>
public interface IGetAvailableSlotsUseCase
{
    Task<DayAvailabilityDto> ExecuteAsync(
        Guid companyId,
        Guid serviceId,
        DateOnly date,
        Guid? professionalId = null,
        CancellationToken cancellationToken = default);
}
