using Google.Cloud.Firestore;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Abstractions;

namespace ReservasSaaS.Infrastructure.Persistence.Firestore.Documents;

[FirestoreData]
public class CompanyMembershipDocument : IFirestoreDocument
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty]
    public string CompanyId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;

    [FirestoreProperty]
    public int Role { get; set; } = (int)CompanyRole.Professional;

    [FirestoreProperty]
    public int Status { get; set; } = (int)MembershipStatus.Active;

    [FirestoreProperty]
    public List<string> CustomPermissions { get; set; } = new();

    [FirestoreProperty]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [FirestoreProperty]
    public DateTime? UpdatedAtUtc { get; set; }
}
