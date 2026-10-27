using ReservasSaaS.Domain.Enums;

namespace ReservasSaaS.Domain.Ports.Outgoing;

/// <summary>
/// Secondary port for probing infrastructure and operating system health.
/// </summary>
public interface ISystemHealthCheckPort
{
    Task<HealthProbeResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}

public record HealthProbeResult(
    HealthStatus Status,
    string Description,
    TimeSpan Duration,
    IReadOnlyDictionary<string, object> Data);
