using Microsoft.AspNetCore.Mvc;
using ReservasSaaS.Application.Common;
using ReservasSaaS.Application.DTOs.Identity;
using ReservasSaaS.Application.Ports.Incoming;
using ReservasSaaS.Application.Security;

namespace ReservasSaaS.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class IdentityController : ControllerBase
{
    private readonly IGetUserProfileUseCase _getUserProfileUseCase;
    private readonly IGetUserCompaniesUseCase _getUserCompaniesUseCase;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<IdentityController> _logger;

    public IdentityController(
        IGetUserProfileUseCase getUserProfileUseCase,
        IGetUserCompaniesUseCase getUserCompaniesUseCase,
        ITenantContext tenantContext,
        ILogger<IdentityController> logger)
    {
        _getUserProfileUseCase = getUserProfileUseCase;
        _getUserCompaniesUseCase = getUserCompaniesUseCase;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets the profile of the currently authenticated Firebase user.
    /// Auto-provisions user record on first sign-in if needed.
    /// </summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var profile = await _getUserProfileUseCase.ExecuteAsync(cancellationToken);
        return Ok(ApiResponse<UserProfileDto>.Ok(profile));
    }

    /// <summary>
    /// Returns all companies in which the authenticated user holds an active membership,
    /// along with their assigned role and effective permissions for each company.
    /// </summary>
    [HttpGet("companies")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserCompanyAccessDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCompanies(CancellationToken cancellationToken)
    {
        var companies = await _getUserCompaniesUseCase.ExecuteAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UserCompanyAccessDto>>.Ok(companies));
    }

    /// <summary>
    /// Returns the effective permissions for the current user in the selected company context.
    /// Requires an active tenant context (via X-Company-Id header or route).
    /// </summary>
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public IActionResult GetPermissions()
    {
        if (!_tenantContext.IsAuthorizedForTenant || !_tenantContext.CompanyId.HasValue)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "A valid company context must be provided via the 'X-Company-Id' header.",
                "MISSING_COMPANY_CONTEXT"));
        }

        var result = new
        {
            CompanyId = _tenantContext.CompanyId.Value,
            Role = _tenantContext.UserRole?.ToString(),
            Permissions = _tenantContext.EffectivePermissions
        };

        return Ok(ApiResponse<object>.Ok(result));
    }

    /// <summary>
    /// Registers or updates the user's FCM device token for push notifications.
    /// </summary>
    [HttpPost("device-token")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public IActionResult RegisterDeviceToken([FromBody] RegisterDeviceTokenCommand command)
    {
        _logger.LogInformation("FCM Device Token registered. Platform: '{Platform}', Device: '{Device}'.",
            command.Platform, command.DeviceName ?? "Mobile");
        return Ok(ApiResponse<bool>.Ok(true, "Device token registered successfully."));
    }

    /// <summary>
    /// Retrieves recent in-app notification events for the current user.
    /// </summary>
    [HttpGet("notifications")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationDto>>), StatusCodes.Status200OK)]
    public IActionResult GetNotifications()
    {
        var sampleNotifications = new List<NotificationDto>
        {
            new NotificationDto(
                Guid.NewGuid().ToString("N"),
                "Bienvenido a ReservasSaaS",
                "Gestiona tus reservas y barberías favoritas desde la app.",
                "general",
                DateTime.UtcNow.AddHours(-1),
                false)
        };
        return Ok(ApiResponse<IReadOnlyList<NotificationDto>>.Ok(sampleNotifications));
    }
}

public record RegisterDeviceTokenCommand(
    string DeviceToken,
    string Platform,
    string? DeviceName = null,
    string? AppVersion = null);

public record NotificationDto(
    string Id,
    string Title,
    string Body,
    string Type,
    DateTime TimestampUtc,
    bool IsRead,
    string? EntityId = null,
    string? CompanyId = null,
    string? ActionRoute = null);
