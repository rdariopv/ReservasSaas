using ReservasSaaS.Domain.Common;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.Security;

namespace ReservasSaaS.Domain.Entities;

/// <summary>
/// Membership association between a User and a Company (Tenant).
/// Enforces multi-tenant authorization and role/permission assignment.
/// </summary>
public class CompanyMembership : BaseEntity<Guid>, IAggregateRoot
{
    public Guid CompanyId { get; private set; }
    public Guid UserId { get; private set; }
    public CompanyRole Role { get; private set; }
    public MembershipStatus Status { get; private set; }
    public HashSet<string> CustomPermissions { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    private CompanyMembership()
    {
    }

    public CompanyMembership(
        Guid id,
        Guid companyId,
        Guid userId,
        CompanyRole role,
        MembershipStatus status = MembershipStatus.Active,
        IEnumerable<string>? customPermissions = null)
        : base(id)
    {
        if (companyId == Guid.Empty)
            throw new ValidationException("CompanyId cannot be empty.");

        if (userId == Guid.Empty)
            throw new ValidationException("UserId cannot be empty.");

        CompanyId = companyId;
        UserId = userId;
        Role = role;
        Status = status;

        if (customPermissions != null)
        {
            CustomPermissions = new HashSet<string>(customPermissions, StringComparer.OrdinalIgnoreCase);
        }

        CreatedAtUtc = DateTime.UtcNow;
    }

    public void ChangeRole(CompanyRole newRole)
    {
        Role = newRole;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ChangeStatus(MembershipStatus newStatus)
    {
        Status = newStatus;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Resolves all effective permissions for this membership by combining the role matrix and custom overrides.
    /// </summary>
    public IReadOnlySet<string> GetEffectivePermissions()
    {
        var rolePermissions = RolePermissionsMatrix.GetPermissionsForRole(Role);
        var effective = new HashSet<string>(rolePermissions, StringComparer.OrdinalIgnoreCase);

        foreach (var custom in CustomPermissions)
        {
            effective.Add(custom);
        }

        return effective;
    }

    public bool HasPermission(string permission)
    {
        if (Status != MembershipStatus.Active)
            return false;

        return GetEffectivePermissions().Contains(permission);
    }
}
