namespace ReservasSaaS.Domain.Enums;

/// <summary>
/// Strategy used by a company to assign professionals to client booking requests.
/// </summary>
public enum ProfessionalAssignmentStrategy
{
    /// <summary>
    /// 1. El cliente selecciona explícitamente el profesional de su preferencia.
    /// </summary>
    ClientSelectsProfessional = 1,

    /// <summary>
    /// 2. El negocio asigna al profesional después de recibir la solicitud (o auto-asigna por disponibilidad óptima).
    /// </summary>
    BusinessAssignsProfessional = 2
}
