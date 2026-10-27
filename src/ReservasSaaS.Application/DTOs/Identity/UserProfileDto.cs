namespace ReservasSaaS.Application.DTOs.Identity;

public record UserProfileDto(
    Guid Id,
    string FirebaseUid,
    string Email,
    string DisplayName,
    string? PhoneNumber,
    string? PhotoUrl,
    bool IsSaaSAdministrator,
    DateTime CreatedAtUtc);
