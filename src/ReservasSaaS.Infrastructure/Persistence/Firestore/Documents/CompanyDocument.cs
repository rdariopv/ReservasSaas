using Google.Cloud.Firestore;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Abstractions;

namespace ReservasSaaS.Infrastructure.Persistence.Firestore.Documents;

[FirestoreData]
public class CompanyDocument : IFirestoreDocument
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Slug { get; set; } = string.Empty;

    [FirestoreProperty]
    public string? TaxId { get; set; }

    [FirestoreProperty]
    public string? TimeZone { get; set; }

    [FirestoreProperty]
    public string? Currency { get; set; }

    [FirestoreProperty]
    public int Status { get; set; } = (int)CompanyStatus.Active;

    [FirestoreProperty]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [FirestoreProperty]
    public DateTime? UpdatedAtUtc { get; set; }
}
