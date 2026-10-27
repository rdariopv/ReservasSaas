namespace ReservasSaaS.Domain.Ports.Outgoing;

public record TimeInterval(DateTime StartUtc, DateTime EndUtc, string? ReferenceId = null);

/// <summary>
/// Secondary port to query already booked/busy intervals for professionals,
/// guaranteeing that neither Flutter nor WhatsApp can book outside real capacity.
/// </summary>
public interface IExistingAppointmentsQueryPort
{
    Task<IReadOnlyList<TimeInterval>> GetBusyIntervalsAsync(
        Guid companyId, 
        Guid professionalId, 
        DateOnly date, 
        CancellationToken cancellationToken = default);
}
