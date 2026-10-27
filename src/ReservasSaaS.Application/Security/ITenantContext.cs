using ReservasSaaS.Domain.Enums;

namespace ReservasSaaS.Application.Security;

/// <summary>
/// Verified tenant execution context. Populated ONLY after server-side validation of company membership.
/// </summary>
public interface ITenantContext
{
    Guid? CompanyId { get; }
    CompanyRole? UserRole { get; }
    IReadOnlySet<string> EffectivePermissions { get; }
    bool IsAuthorizedForTenant { get; }
    bool HasPermission(string permission);
}
