using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReservasSaaS.Domain.Exceptions;

namespace ReservasSaaS.Infrastructure.Authentication;

public class FirebaseTokenValidator : IFirebaseTokenValidator
{
    private readonly FirebaseAuthenticationOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<FirebaseTokenValidator> _logger;
    private readonly JwtSecurityTokenHandler _jwtHandler = new();

    public FirebaseTokenValidator(
        IOptions<FirebaseAuthenticationOptions> options,
        IHostEnvironment environment,
        ILogger<FirebaseTokenValidator> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public Task<ClaimsPrincipal> ValidateTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new UnauthorizedException("Authorization token is missing.");
        }

        // Support simulated/mock tokens in development or testing environments
        if ((_environment.IsDevelopment() || _options.AllowSimulatedTokensInDevelopment) && 
            idToken.StartsWith("mock-firebase-token:", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(CreateSimulatedPrincipal(idToken));
        }

        try
        {
            // Parse JWT
            if (!_jwtHandler.CanReadToken(idToken))
            {
                throw new UnauthorizedException("The provided token is not a valid JWT.");
            }

            var jwt = _jwtHandler.ReadJwtToken(idToken);

            // Validate Firebase Issuer if ProjectId is configured
            if (!string.IsNullOrWhiteSpace(_options.ProjectId))
            {
                if (!string.Equals(jwt.Issuer, _options.Issuer, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Token issuer '{Issuer}' does not match expected Firebase issuer '{Expected}'",
                        jwt.Issuer, _options.Issuer);
                    throw new UnauthorizedException("Token issuer is not valid for this Firebase project.");
                }

                if (!jwt.Audiences.Contains(_options.Audience, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Token audience does not match configured Firebase ProjectId '{ProjectId}'", _options.ProjectId);
                    throw new UnauthorizedException("Token audience is not valid for this application.");
                }
            }

            // Verify expiration
            if (jwt.ValidTo < DateTime.UtcNow)
            {
                throw new UnauthorizedException("Firebase ID token has expired.");
            }

            // Extract claims
            var uid = jwt.Claims.FirstOrDefault(c => c.Type == "user_id" || c.Type == "sub")?.Value;
            if (string.IsNullOrWhiteSpace(uid))
            {
                throw new UnauthorizedException("Firebase ID token is missing the user_id (sub) claim.");
            }

            var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? email?.Split('@')[0] ?? uid;
            var isSaasAdminClaim = jwt.Claims.FirstOrDefault(c => c.Type == "saas_admin")?.Value;
            var isSaasAdmin = bool.TryParse(isSaasAdminClaim, out var adminVal) && adminVal;

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, uid),
                new("firebase_uid", uid),
                new(ClaimTypes.Name, name)
            };

            if (!string.IsNullOrWhiteSpace(email))
            {
                claims.Add(new Claim(ClaimTypes.Email, email));
            }

            if (isSaasAdmin)
            {
                claims.Add(new Claim("is_saas_admin", "true"));
                claims.Add(new Claim(ClaimTypes.Role, "SaaSAdministrator"));
            }

            var identity = new ClaimsIdentity(claims, "Firebase");
            return Task.FromResult(new ClaimsPrincipal(identity));
        }
        catch (UnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Firebase token");
            throw new UnauthorizedException("Invalid Firebase token signature or payload.");
        }
    }

    private ClaimsPrincipal CreateSimulatedPrincipal(string mockToken)
    {
        // Format: mock-firebase-token:<uid>:<email>:<isSaasAdmin>
        var parts = mockToken.Split(':');
        var uid = parts.Length > 1 ? parts[1] : "mock-user-1";
        var email = parts.Length > 2 ? parts[2] : $"{uid}@demo.local";
        var isSaasAdmin = parts.Length > 3 && bool.TryParse(parts[3], out var b) && b;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, uid),
            new("firebase_uid", uid),
            new(ClaimTypes.Name, email.Split('@')[0]),
            new(ClaimTypes.Email, email)
        };

        if (isSaasAdmin)
        {
            claims.Add(new Claim("is_saas_admin", "true"));
            claims.Add(new Claim(ClaimTypes.Role, "SaaSAdministrator"));
        }

        _logger.LogInformation("Authenticated simulated Firebase user: {Uid} ({Email}), IsSaaSAdmin: {IsAdmin}", uid, email, isSaasAdmin);

        var identity = new ClaimsIdentity(claims, "SimulatedFirebase");
        return new ClaimsPrincipal(identity);
    }
}
