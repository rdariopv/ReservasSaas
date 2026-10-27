using ReservasSaaS.Application.DTOs.Agenda;

namespace ReservasSaaS.Application.Ports.Incoming;

public interface IManageBookingSettingsUseCase
{
    Task<BookingSettingsDto> GetSettingsAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<BookingSettingsDto> UpdateSettingsAsync(Guid companyId, UpdateBookingSettingsCommand command, CancellationToken cancellationToken = default);
}
