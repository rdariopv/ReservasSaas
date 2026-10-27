using Google.Cloud.Firestore;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Abstractions;

namespace ReservasSaaS.Infrastructure.Persistence.Firestore.Documents;

[FirestoreData]
public class UserDocument : IFirestoreDocument
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty]
    public string FirebaseUid { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Email { get; set; } = string.Empty;

    [FirestoreProperty]
    public string DisplayName { get; set; } = string.Empty;

    [FirestoreProperty]
    public string? PhoneNumber { get; set; }

    [FirestoreProperty]
    public string? PhotoUrl { get; set; }

    [FirestoreProperty]
    public bool IsSaaSAdministrator { get; set; }

    [FirestoreProperty]
    public bool IsActive { get; set; } = true;

    [FirestoreProperty]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [FirestoreProperty]
    public DateTime? UpdatedAtUtc { get; set; }
}
