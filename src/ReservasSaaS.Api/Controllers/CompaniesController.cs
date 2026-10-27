using Microsoft.AspNetCore.Mvc;
using ReservasSaaS.Application.Common;
using ReservasSaaS.Application.DTOs.Identity;
using ReservasSaaS.Application.Ports.Incoming;

namespace ReservasSaaS.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class CompaniesController : ControllerBase
{
    private readonly ICreateCompanyUseCase _createCompanyUseCase;
    private readonly IAuthorizeCompanyAccessUseCase _authorizer;
    private readonly IGetCompanyMembersUseCase _getCompanyMembersUseCase;
    private readonly ILogger<CompaniesController> _logger;

    public CompaniesController(
        ICreateCompanyUseCase createCompanyUseCase,
        IAuthorizeCompanyAccessUseCase authorizer,
        IGetCompanyMembersUseCase getCompanyMembersUseCase,
        ILogger<CompaniesController> logger)
    {
        _createCompanyUseCase = createCompanyUseCase;
        _authorizer = authorizer;
        _getCompanyMembersUseCase = getCompanyMembersUseCase;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new company (tenant) in the platform.
    /// The authenticated creator automatically becomes the Owner.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateCompany(
        [FromBody] CreateCompanyCommand command,
        CancellationToken cancellationToken)
    {
        var company = await _createCompanyUseCase.ExecuteAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetCompanyById), new { companyId = company.Id }, ApiResponse<CompanyDto>.Ok(company));
    }

    /// <summary>
    /// Retrieves company details. Access is restricted to members of this specific company or SaaS Administrators.
    /// Cross-tenant requests are strictly rejected with 403 Forbidden on the server.
    /// </summary>
    [HttpGet("{companyId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompanyById(Guid companyId, CancellationToken cancellationToken)
    {
        var auth = await _authorizer.AuthorizeAsync(companyId, null, cancellationToken);
        var company = auth.Company;

        var dto = new CompanyDto(
            Id: company.Id,
            Name: company.Name,
            Slug: company.Slug,
            TaxId: company.TaxId,
            TimeZone: company.TimeZone ?? "America/Bogota",
            Currency: company.Currency ?? "COP",
            Status: company.Status,
            CreatedAtUtc: company.CreatedAtUtc
        );

        return Ok(ApiResponse<CompanyDto>.Ok(dto));
    }

    /// <summary>
    /// Retrieves the list of staff and members in a company.
    /// Requires MembersRead permission (Owner, Admin, or Reception).
    /// </summary>
    [HttpGet("{companyId:guid}/members")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CompanyMemberDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompanyMembers(Guid companyId, CancellationToken cancellationToken)
    {
        var members = await _getCompanyMembersUseCase.ExecuteAsync(companyId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CompanyMemberDto>>.Ok(members));
    }
}
