namespace ReservasSaaS.Domain.Security;

/// <summary>
/// Fine-grained permission strings for role-based and permission-based authorization.
/// </summary>
public static class Permissions
{
    // Company / Tenant management
    public const string CompanyRead = "company:read";
    public const string CompanyUpdate = "company:update";
    public const string CompanyDelete = "company:delete";
    public const string CompanySettings = "company:settings";

    // Staff & Memberships
    public const string MembersRead = "members:read";
    public const string MembersInvite = "members:invite";
    public const string MembersUpdateRole = "members:update_role";
    public const string MembersRemove = "members:remove";

    // Services & Resources
    public const string ServicesRead = "services:read";
    public const string ServicesManage = "services:manage";

    // Schedule & Appointments (foundational permissions for upcoming phases)
    public const string AppointmentsRead = "appointments:read";
    public const string AppointmentsManage = "appointments:manage";
    public const string AppointmentsSelfOnly = "appointments:self_only";

    // Audit and Reports
    public const string ReportsRead = "reports:read";
    public const string BillingManage = "billing:manage";

    // SaaS Administrator platform privileges
    public const string GlobalTenantManage = "global:tenants:manage";
    public const string GlobalPlatformAudit = "global:platform:audit";
}
