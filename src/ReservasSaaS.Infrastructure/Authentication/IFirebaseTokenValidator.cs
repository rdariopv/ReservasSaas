using System.Security.Claims;

namespace ReservasSaaS.Infrastructure.Authentication;

public interface IFirebaseTokenValidator
{
    Task<ClaimsPrincipal> ValidateTokenAsync(string idToken, CancellationToken cancellationToken = default);
}
