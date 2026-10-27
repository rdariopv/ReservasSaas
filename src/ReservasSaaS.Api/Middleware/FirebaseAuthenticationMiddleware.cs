using System.Net.Http.Headers;
using ReservasSaaS.Infrastructure.Authentication;

namespace ReservasSaaS.Api.Middleware;

public class FirebaseAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FirebaseAuthenticationMiddleware> _logger;

    public FirebaseAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<FirebaseAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IFirebaseTokenValidator tokenValidator)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrWhiteSpace(authHeader) && 
            AuthenticationHeaderValue.TryParse(authHeader, out var parsedHeader) &&
            string.Equals(parsedHeader.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(parsedHeader.Parameter))
        {
            try
            {
                var principal = await tokenValidator.ValidateTokenAsync(parsedHeader.Parameter, context.RequestAborted);
                context.User = principal;
                _logger.LogDebug("Successfully authenticated request for user '{User}' via Firebase", principal.Identity?.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to validate Bearer token: {Message}", ex.Message);
                // The global exception handler or subsequent authorization check will handle unauthenticated states
            }
        }

        await _next(context);
    }
}
