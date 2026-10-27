using ReservasSaaS.Domain.Enums;

namespace ReservasSaaS.Domain.Security;

/// <summary>
/// Domain-driven authority matrix mapping company roles to default granular permissions.
/// </summary>
public static class RolePermissionsMatrix
{
    private static readonly Dictionary<CompanyRole, HashSet<string>> RolePermissions = new()
    {
        [CompanyRole.SaaSAdministrator] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Permissions.GlobalTenantManage,
            Permissions.GlobalPlatformAudit,
            Permissions.CompanyRead,
            Permissions.CompanyUpdate,
            Permissions.CompanyDelete,
            Permissions.CompanySettings,
            Permissions.MembersRead,
            Permissions.MembersInvite,
            Permissions.MembersUpdateRole,
            Permissions.MembersRemove,
            Permissions.ServicesRead,
            Permissions.ServicesManage,
            Permissions.AppointmentsRead,
            Permissions.AppointmentsManage,
            Permissions.ReportsRead,
            Permissions.BillingManage
        },

        [CompanyRole.Owner] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Permissions.CompanyRead,
            Permissions.CompanyUpdate,
            Permissions.CompanyDelete,
            Permissions.CompanySettings,
            Permissions.MembersRead,
            Permissions.MembersInvite,
            Permissions.MembersUpdateRole,
            Permissions.MembersRemove,
            Permissions.ServicesRead,
            Permissions.ServicesManage,
            Permissions.AppointmentsRead,
            Permissions.AppointmentsManage,
            Permissions.ReportsRead,
            Permissions.BillingManage
        },

        [CompanyRole.Admin] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Permissions.CompanyRead,
            Permissions.CompanyUpdate,
            Permissions.CompanySettings,
            Permissions.MembersRead,
            Permissions.MembersInvite,
            Permissions.ServicesRead,
            Permissions.ServicesManage,
            Permissions.AppointmentsRead,
            Permissions.AppointmentsManage,
            Permissions.ReportsRead
        },

        [CompanyRole.Reception] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Permissions.CompanyRead,
            Permissions.MembersRead,
            Permissions.ServicesRead,
            Permissions.AppointmentsRead,
            Permissions.AppointmentsManage
        },

        [CompanyRole.Professional] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Permissions.CompanyRead,
            Permissions.ServicesRead,
            Permissions.AppointmentsSelfOnly
        }
    };

    public static IReadOnlySet<string> GetPermissionsForRole(CompanyRole role)
    {
        if (RolePermissions.TryGetValue(role, out var permissions))
        {
            return permissions;
        }

        return new HashSet<string>();
    }

    public static bool HasPermission(CompanyRole role, string permission)
    {
        return RolePermissions.TryGetValue(role, out var permissions) && permissions.Contains(permission);
    }
}
