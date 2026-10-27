namespace ReservasSaaS.Domain.Exceptions;

/// <summary>
/// Thrown when an authenticated user attempts to access a company or resource for which they lack membership or permissions.
/// Critical for cross-tenant isolation enforcement.
/// </summary>
public class ForbiddenException : DomainException
{
    public string? CompanyId { get; }
    public string? RequiredPermission { get; }

    public ForbiddenException(string message) 
        : base(message, "FORBIDDEN")
    {
    }

    public ForbiddenException(string companyId, string? requiredPermission = null)
        : base(requiredPermission == null 
            ? $"Access to company '{companyId}' was denied. The user has no active membership."
            : $"Access to company '{companyId}' was denied. Missing required permission '{requiredPermission}'.",
            "FORBIDDEN")
    {
        CompanyId = companyId;
        RequiredPermission = requiredPermission;
    }
}
