namespace ReservasSaaS.Application.Security;

/// <summary>
/// Provides access to the verified authenticated user extracted from the Firebase ID token.
/// </summary>
public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    string? FirebaseUid { get; }
    string? Email { get; }
    Guid? UserId { get; }
    bool IsSaaSAdministrator { get; }
}
