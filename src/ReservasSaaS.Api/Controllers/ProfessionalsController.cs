using Microsoft.AspNetCore.Mvc;
using ReservasSaaS.Application.Common;
using ReservasSaaS.Application.DTOs.Agenda;
using ReservasSaaS.Application.Ports.Incoming;

namespace ReservasSaaS.Api.Controllers;

[ApiController]
[Route("api/v1/companies/{companyId:guid}/[controller]")]
[Produces("application/json")]
public class ProfessionalsController : ControllerBase
{
    private readonly IManageProfessionalsUseCase _professionalsUseCase;
    private readonly ILogger<ProfessionalsController> _logger;

    public ProfessionalsController(
        IManageProfessionalsUseCase professionalsUseCase,
        ILogger<ProfessionalsController> logger)
    {
        _professionalsUseCase = professionalsUseCase;
        _logger = logger;
    }

    /// <summary>
    /// Lists all professionals associated with a company.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProfessionalDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfessionals(Guid companyId, CancellationToken cancellationToken)
    {
        var professionals = await _professionalsUseCase.GetCompanyProfessionalsAsync(companyId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProfessionalDto>>.Ok(professionals));
    }

    /// <summary>
    /// Retrieves full profile, working hours, breaks, and exceptions for a specific professional.
    /// </summary>
    [HttpGet("{professionalId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProfessionalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfessionalById(
        Guid companyId, 
        Guid professionalId, 
        CancellationToken cancellationToken)
    {
        var professional = await _professionalsUseCase.GetProfessionalByIdAsync(companyId, professionalId, cancellationToken);
        return Ok(ApiResponse<ProfessionalDto>.Ok(professional));
    }

    /// <summary>
    /// Registers a new professional and links them to an authorized user identity when applicable.
    /// Requires MembersInvite permission.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProfessionalDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateProfessional(
        Guid companyId, 
        [FromBody] CreateProfessionalCommand command, 
        CancellationToken cancellationToken)
    {
        var professional = await _professionalsUseCase.CreateProfessionalAsync(companyId, command, cancellationToken);
        return CreatedAtAction(nameof(GetProfessionalById), new { companyId, professionalId = professional.Id }, ApiResponse<ProfessionalDto>.Ok(professional));
    }

    /// <summary>
    /// Updates working schedules, breaks, and date-specific exceptions for a professional.
    /// Requires AppointmentsManage permission.
    /// </summary>
    [HttpPut("{professionalId:guid}/schedule")]
    [ProducesResponseType(typeof(ApiResponse<ProfessionalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSchedule(
        Guid companyId, 
        Guid professionalId, 
        [FromBody] UpdateProfessionalScheduleCommand command, 
        CancellationToken cancellationToken)
    {
        var updated = await _professionalsUseCase.UpdateScheduleAsync(companyId, professionalId, command, cancellationToken);
        return Ok(ApiResponse<ProfessionalDto>.Ok(updated));
    }
}
