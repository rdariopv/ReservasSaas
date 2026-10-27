using Microsoft.AspNetCore.Mvc;
using ReservasSaaS.Application.Common;
using ReservasSaaS.Application.DTOs;
using ReservasSaaS.Application.Ports.Incoming;

namespace ReservasSaaS.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly IGetSystemHealthUseCase _getSystemHealthUseCase;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IGetSystemHealthUseCase getSystemHealthUseCase,
        ILogger<HealthController> logger)
    {
        _getSystemHealthUseCase = getSystemHealthUseCase;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates system readiness, environment configurations, and hexagonal architecture ports.
    /// </summary>
    /// <returns>System health status, uptime, environment, and subsystem diagnostics.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HealthCheckDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HealthCheckDto>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing GET /health request");

        var health = await _getSystemHealthUseCase.ExecuteAsync(cancellationToken);

        var response = ApiResponse<HealthCheckDto>.Ok(health, "System is operational.");

        if (health.Status == "Unhealthy")
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }

        return Ok(response);
    }
}
