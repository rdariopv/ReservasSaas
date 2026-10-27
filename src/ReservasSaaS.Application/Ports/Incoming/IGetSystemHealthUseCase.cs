using ReservasSaaS.Application.DTOs;

namespace ReservasSaaS.Application.Ports.Incoming;

/// <summary>
/// Primary / Inbound Port: Contract to query system health and readiness.
/// </summary>
public interface IGetSystemHealthUseCase
{
    Task<HealthCheckDto> ExecuteAsync(CancellationToken cancellationToken = default);
}
