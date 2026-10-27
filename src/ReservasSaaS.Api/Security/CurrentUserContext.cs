using System.Security.Claims;
using ReservasSaaS.Application.Security;

namespace ReservasSaaS.Api.Security;

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public string? FirebaseUid =>
        User?.FindFirst("firebase_uid")?.Value ??
        User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public string? Email =>
        User?.FindFirst(ClaimTypes.Email)?.Value;

    public Guid? UserId
    {
        get
        {
            var val = User?.FindFirst("user_id")?.Value;
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    public bool IsSaaSAdministrator =>
        User?.IsInRole("SaaSAdministrator") == true ||
        string.Equals(User?.FindFirst("is_saas_admin")?.Value, "true", StringComparison.OrdinalIgnoreCase);
}
