using Microsoft.AspNetCore.Mvc;
using ReservasSaaS.Application.Common;
using ReservasSaaS.Application.DTOs.Agenda;
using ReservasSaaS.Application.Ports.Incoming;

namespace ReservasSaaS.Api.Controllers;

[ApiController]
[Route("api/v1/companies/{companyId:guid}/settings/booking")]
[Produces("application/json")]
public class CompanyBookingSettingsController : ControllerBase
{
    private readonly IManageBookingSettingsUseCase _settingsUseCase;

    public CompanyBookingSettingsController(IManageBookingSettingsUseCase settingsUseCase)
    {
        _settingsUseCase = settingsUseCase;
    }

    /// <summary>
    /// Gets the company's booking and professional assignment settings.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<BookingSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSettings(Guid companyId, CancellationToken cancellationToken)
    {
        var settings = await _settingsUseCase.GetSettingsAsync(companyId, cancellationToken);
        return Ok(ApiResponse<BookingSettingsDto>.Ok(settings));
    }

    /// <summary>
    /// Configures the professional assignment strategy:
    /// 1. ClientSelectsProfessional (Client picks their preferred professional)
    /// 2. BusinessAssignsProfessional (Business/system assigns professional based on availability)
    /// Requires CompanySettings permission.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<BookingSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSettings(
        Guid companyId, 
        [FromBody] UpdateBookingSettingsCommand command, 
        CancellationToken cancellationToken)
    {
        var updated = await _settingsUseCase.UpdateSettingsAsync(companyId, command, cancellationToken);
        return Ok(ApiResponse<BookingSettingsDto>.Ok(updated));
    }
}
