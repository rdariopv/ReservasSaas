using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ReservasSaaS.Application.DTOs;
using ReservasSaaS.Application.Ports.Incoming;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Ports.Outgoing;

namespace ReservasSaaS.Application.UseCases;

/// <summary>
/// Implements the system health evaluation use case following Hexagonal Architecture.
/// Orchestrates outgoing ports (DateTimeProvider, SystemHealthCheckPort).
/// </summary>
public class GetSystemHealthUseCase : IGetSystemHealthUseCase
{
    private static readonly DateTime StartTimeUtc = DateTime.UtcNow;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISystemHealthCheckPort _healthCheckPort;
    private readonly ILogger<GetSystemHealthUseCase> _logger;

    public GetSystemHealthUseCase(
        IDateTimeProvider dateTimeProvider,
        ISystemHealthCheckPort healthCheckPort,
        ILogger<GetSystemHealthUseCase> logger)
    {
        _dateTimeProvider = dateTimeProvider;
        _healthCheckPort = healthCheckPort;
        _logger = logger;
    }

    public async Task<HealthCheckDto> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing GetSystemHealthUseCase verification...");

        var currentUtc = _dateTimeProvider.UtcNow;
        var uptime = currentUtc - StartTimeUtc;

        var probeResult = await _healthCheckPort.CheckHealthAsync(cancellationToken);

        var subsystems = new Dictionary<string, SubsystemHealthDto>
        {
            ["OperatingSystem"] = new SubsystemHealthDto(
                Status: probeResult.Status.ToString(),
                Description: probeResult.Description,
                DurationMilliseconds: (long)probeResult.Duration.TotalMilliseconds,
                Details: probeResult.Data
            ),
            ["HexagonalPorts"] = new SubsystemHealthDto(
                Status: HealthStatus.Healthy.ToString(),
                Description: "Domain and Application ports bound successfully via dependency injection",
                DurationMilliseconds: 1,
                Details: new Dictionary<string, object>
                {
                    ["PrimaryPort"] = "IGetSystemHealthUseCase",
                    ["SecondaryPort_Clock"] = "IDateTimeProvider",
                    ["SecondaryPort_Diagnostics"] = "ISystemHealthCheckPort"
                }
            )
        };

        var overallStatus = probeResult.Status == HealthStatus.Healthy ? "Healthy" : probeResult.Status.ToString();

        var result = new HealthCheckDto(
            Status: overallStatus,
            Environment: Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            Version: "1.0.0-phase1",
            TimestampUtc: currentUtc,
            Uptime: uptime,
            Architecture: "Hexagonal / Clean Architecture (.NET 10)",
            Subsystems: subsystems
        );

        _logger.LogInformation("System health evaluated. Overall status: {Status}, Uptime: {Uptime}", overallStatus, uptime);

        return result;
    }
}
