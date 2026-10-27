namespace ReservasSaaS.Domain.Enums;

/// <summary>
/// Domain-level health status representation.
/// </summary>
public enum HealthStatus
{
    Healthy = 1,
    Degraded = 2,
    Unhealthy = 3
}
