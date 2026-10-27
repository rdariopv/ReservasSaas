using ReservasSaaS.Domain.Enums;

namespace ReservasSaaS.Application.DTOs;

/// <summary>
/// Detailed DTO returned by the system health check use case.
/// </summary>
public record HealthCheckDto(
    string Status,
    string Environment,
    string Version,
    DateTime TimestampUtc,
    TimeSpan Uptime,
    string Architecture,
    IReadOnlyDictionary<string, SubsystemHealthDto> Subsystems);

public record SubsystemHealthDto(
    string Status,
    string Description,
    long DurationMilliseconds,
    IReadOnlyDictionary<string, object> Details);
