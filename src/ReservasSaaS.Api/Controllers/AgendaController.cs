using Microsoft.AspNetCore.Mvc;
using ReservasSaaS.Application.Common;
using ReservasSaaS.Application.DTOs.Agenda;
using ReservasSaaS.Application.Ports.Incoming;

namespace ReservasSaaS.Api.Controllers;

[ApiController]
[Route("api/v1/companies/{companyId:guid}/[controller]")]
[Produces("application/json")]
public class AgendaController : ControllerBase
{
    private readonly IGetAvailableSlotsUseCase _getAvailableSlotsUseCase;
    private readonly ILogger<AgendaController> _logger;

    public AgendaController(
        IGetAvailableSlotsUseCase getAvailableSlotsUseCase,
        ILogger<AgendaController> logger)
    {
        _getAvailableSlotsUseCase = getAvailableSlotsUseCase;
        _logger = logger;
    }

    /// <summary>
    /// Centralized availability engine for scheduling slots.
    /// Calculates available booking slots from shifts, service duration, buffers, recurring breaks,
    /// and existing booked appointments to prevent overbooking outside real capacity.
    /// Used identically by Flutter mobile/web apps and WhatsApp bot webhook.
    /// </summary>
    /// <param name="companyId">Target company identifier</param>
    /// <param name="serviceId">Service being scheduled (determines duration and buffers)</param>
    /// <param name="date">Target date in YYYY-MM-DD format</param>
    /// <param name="professionalId">Optional professional identifier (if client selected, or null for business/any assignment)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("available-slots")]
    [ProducesResponseType(typeof(ApiResponse<DayAvailabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailableSlots(
        Guid companyId,
        [FromQuery] Guid serviceId,
        [FromQuery] string date,
        [FromQuery] Guid? professionalId = null,
        CancellationToken cancellationToken = default)
    {
        if (serviceId == Guid.Empty)
        {
            return BadRequest(ApiResponse<DayAvailabilityDto>.Fail("A valid serviceId parameter is required.", "INVALID_SERVICE_ID"));
        }

        if (!DateOnly.TryParse(date, out var parsedDate))
        {
            return BadRequest(ApiResponse<DayAvailabilityDto>.Fail("The 'date' parameter must be in 'yyyy-MM-dd' format.", "INVALID_DATE_FORMAT"));
        }

        _logger.LogInformation(
            "Querying available slots for Company '{CompanyId}', Service '{ServiceId}', Date '{Date}', Professional '{ProfId}'",
            companyId, serviceId, parsedDate, professionalId);

        var availability = await _getAvailableSlotsUseCase.ExecuteAsync(
            companyId: companyId,
            serviceId: serviceId,
            date: parsedDate,
            professionalId: professionalId,
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<DayAvailabilityDto>.Ok(availability));
    }
}
