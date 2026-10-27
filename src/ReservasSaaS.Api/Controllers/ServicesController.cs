using Microsoft.AspNetCore.Mvc;
using ReservasSaaS.Application.Common;
using ReservasSaaS.Application.DTOs.Agenda;
using ReservasSaaS.Application.Ports.Incoming;

namespace ReservasSaaS.Api.Controllers;

[ApiController]
[Route("api/v1/companies/{companyId:guid}/[controller]")]
[Produces("application/json")]
public class ServicesController : ControllerBase
{
    private readonly IManageServicesUseCase _servicesUseCase;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(
        IManageServicesUseCase servicesUseCase,
        ILogger<ServicesController> logger)
    {
        _servicesUseCase = servicesUseCase;
        _logger = logger;
    }

    /// <summary>
    /// Lists all services configured for a company.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ServiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServices(
        Guid companyId, 
        [FromQuery] bool onlyActive = false, 
        CancellationToken cancellationToken = default)
    {
        var services = await _servicesUseCase.GetCompanyServicesAsync(companyId, onlyActive, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ServiceDto>>.Ok(services));
    }

    /// <summary>
    /// Retrieves details of a specific service.
    /// </summary>
    [HttpGet("{serviceId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServiceById(
        Guid companyId, 
        Guid serviceId, 
        CancellationToken cancellationToken)
    {
        var service = await _servicesUseCase.GetServiceByIdAsync(companyId, serviceId, cancellationToken);
        return Ok(ApiResponse<ServiceDto>.Ok(service));
    }

    /// <summary>
    /// Creates a new service with duration, price, buffers, and deposit policy.
    /// Requires ServicesManage permission.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateService(
        Guid companyId, 
        [FromBody] CreateServiceCommand command, 
        CancellationToken cancellationToken)
    {
        var service = await _servicesUseCase.CreateServiceAsync(companyId, command, cancellationToken);
        return CreatedAtAction(nameof(GetServiceById), new { companyId, serviceId = service.Id }, ApiResponse<ServiceDto>.Ok(service));
    }

    /// <summary>
    /// Updates an existing service.
    /// Requires ServicesManage permission.
    /// </summary>
    [HttpPut("{serviceId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateService(
        Guid companyId, 
        Guid serviceId, 
        [FromBody] UpdateServiceCommand command, 
        CancellationToken cancellationToken)
    {
        var service = await _servicesUseCase.UpdateServiceAsync(companyId, serviceId, command, cancellationToken);
        return Ok(ApiResponse<ServiceDto>.Ok(service));
    }

    /// <summary>
    /// Archives a service (soft-delete).
    /// Requires ServicesManage permission.
    /// </summary>
    [HttpDelete("{serviceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteService(
        Guid companyId, 
        Guid serviceId, 
        CancellationToken cancellationToken)
    {
        await _servicesUseCase.DeleteServiceAsync(companyId, serviceId, cancellationToken);
        return NoContent();
    }
}
