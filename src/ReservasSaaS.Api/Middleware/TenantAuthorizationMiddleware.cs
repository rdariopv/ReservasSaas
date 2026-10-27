using ReservasSaaS.Application.Ports.Incoming;
using ReservasSaaS.Domain.Exceptions;

namespace ReservasSaaS.Api.Middleware;

/// <summary>
/// Server-side tenant authorization middleware.
/// Intercepts any request specifying a company identifier (via route parameter or X-Company-Id header)
/// and enforces membership validation in the server's repository before allowing execution.
/// </summary>
public class TenantAuthorizationMiddleware
{
    public const string CompanyIdHeader = "X-Company-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantAuthorizationMiddleware> _logger;

    public TenantAuthorizationMiddleware(
        RequestDelegate next,
        ILogger<TenantAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuthorizeCompanyAccessUseCase authorizer)
    {
        // 1. Check if route or header specifies a company context
        Guid? targetCompanyId = null;

        if (context.Request.RouteValues.TryGetValue("companyId", out var routeVal) && 
            routeVal != null && 
            Guid.TryParse(routeVal.ToString(), out var parsedRouteId))
        {
            targetCompanyId = parsedRouteId;
        }
        else if (context.Request.Headers.TryGetValue(CompanyIdHeader, out var headerVal) && 
                 Guid.TryParse(headerVal.ToString(), out var parsedHeaderId))
        {
            targetCompanyId = parsedHeaderId;
        }

        // 2. If a company context is present, validate server-side authorization
        if (targetCompanyId.HasValue && targetCompanyId.Value != Guid.Empty)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("Tenant access to '{CompanyId}' rejected: unauthenticated", targetCompanyId.Value);
                throw new UnauthorizedException("Authentication is required to access company resources.");
            }

            var authResult = await authorizer.AuthorizeAsync(targetCompanyId.Value, null, context.RequestAborted);
            context.Items["TenantContext"] = authResult;

            _logger.LogDebug(
                "Tenant authorization validated for Company '{CompanyId}', Role: '{Role}'",
                targetCompanyId.Value, authResult.Role);
        }

        await _next(context);
    }
}
