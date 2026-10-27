using ReservasSaaS.Domain.Common;
using ReservasSaaS.Domain.Exceptions;

namespace ReservasSaaS.Domain.Entities;

/// <summary>
/// User identity entity bound to a Firebase Authentication UID.
/// A user can have memberships in multiple companies.
/// </summary>
public class User : BaseEntity<Guid>, IAggregateRoot
{
    public string FirebaseUid { get; private set; }
    public string Email { get; private set; }
    public string DisplayName { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? PhotoUrl { get; private set; }
    public bool IsSaaSAdministrator { get; private set; }
    public bool IsActive { get; private set; }

    private User()
    {
        FirebaseUid = string.Empty;
        Email = string.Empty;
        DisplayName = string.Empty;
    }

    public User(
        Guid id, 
        string firebaseUid, 
        string email, 
        string displayName, 
        string? phoneNumber = null, 
        string? photoUrl = null,
        bool isSaaSAdministrator = false)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
            throw new ValidationException("Firebase UID cannot be empty.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("User email cannot be empty.");

        FirebaseUid = firebaseUid.Trim();
        Email = email.Trim().ToLowerInvariant();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? email.Split('@')[0] : displayName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        PhotoUrl = photoUrl?.Trim();
        IsSaaSAdministrator = isSaaSAdministrator;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateProfile(string displayName, string? phoneNumber, string? photoUrl)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ValidationException("Display name cannot be empty.");

        DisplayName = displayName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        PhotoUrl = photoUrl?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetSaaSAdministrator(bool isSaaSAdmin)
    {
        IsSaaSAdministrator = isSaaSAdmin;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
