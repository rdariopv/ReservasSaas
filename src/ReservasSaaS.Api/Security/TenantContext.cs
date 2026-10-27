using ReservasSaaS.Application.Ports.Incoming;
using ReservasSaaS.Application.Security;
using ReservasSaaS.Domain.Enums;

namespace ReservasSaaS.Api.Security;

public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private CompanyAuthorizationResult? AuthResult =>
        _httpContextAccessor.HttpContext?.Items["TenantContext"] as CompanyAuthorizationResult;

    public Guid? CompanyId => AuthResult?.Company?.Id;

    public CompanyRole? UserRole => AuthResult?.Role;

    public IReadOnlySet<string> EffectivePermissions =>
        AuthResult?.EffectivePermissions ?? new HashSet<string>();

    public bool IsAuthorizedForTenant => AuthResult?.IsAuthorized == true;

    public bool HasPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission)) return false;
        return EffectivePermissions.Contains(permission);
    }
}
