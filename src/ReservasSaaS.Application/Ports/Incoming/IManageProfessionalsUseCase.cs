using ReservasSaaS.Application.DTOs.Agenda;

namespace ReservasSaaS.Application.Ports.Incoming;

public interface IManageProfessionalsUseCase
{
    Task<IReadOnlyList<ProfessionalDto>> GetCompanyProfessionalsAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<ProfessionalDto> GetProfessionalByIdAsync(Guid companyId, Guid professionalId, CancellationToken cancellationToken = default);
    Task<ProfessionalDto> CreateProfessionalAsync(Guid companyId, CreateProfessionalCommand command, CancellationToken cancellationToken = default);
    Task<ProfessionalDto> UpdateScheduleAsync(Guid companyId, Guid professionalId, UpdateProfessionalScheduleCommand command, CancellationToken cancellationToken = default);
}
