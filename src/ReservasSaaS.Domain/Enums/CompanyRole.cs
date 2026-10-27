namespace ReservasSaaS.Domain.Enums;

/// <summary>
/// Predefined roles in the SaaS platform.
/// </summary>
public enum CompanyRole
{
    /// <summary>
    /// Global super-administrator with cross-company system visibility.
    /// </summary>
    SaaSAdministrator = 1,

    /// <summary>
    /// Legal owner of the company/tenant with full administrative, billing, and membership control.
    /// </summary>
    Owner = 2,

    /// <summary>
    /// Operational administrator responsible for day-to-day settings and staff management.
    /// </summary>
    Admin = 3,

    /// <summary>
    /// Front-desk receptionist handling client intake, scheduling, and customer support.
    /// </summary>
    Reception = 4,

    /// <summary>
    /// Service provider / specialist (e.g., doctor, coach, hairdresser) seeing their schedule and appointments.
    /// </summary>
    Professional = 5
}
