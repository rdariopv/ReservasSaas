using Google.Cloud.Firestore;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Abstractions;

namespace ReservasSaaS.Infrastructure.Persistence.Firestore.Documents;

[FirestoreData]
public class ServiceDocument : IFirestoreDocument
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty]
    public string CompanyId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Description { get; set; } = string.Empty;

    [FirestoreProperty]
    public int DurationMinutes { get; set; }

    [FirestoreProperty]
    public int BufferBeforeMinutes { get; set; }

    [FirestoreProperty]
    public int BufferAfterMinutes { get; set; }

    [FirestoreProperty]
    public double Price { get; set; }

    [FirestoreProperty]
    public string Currency { get; set; } = "COP";

    [FirestoreProperty]
    public int Status { get; set; } = (int)ServiceStatus.Active;

    [FirestoreProperty]
    public bool RequiresDeposit { get; set; }

    [FirestoreProperty]
    public int DepositType { get; set; }

    [FirestoreProperty]
    public double DepositValue { get; set; }

    [FirestoreProperty]
    public string DepositDescription { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [FirestoreProperty]
    public DateTime? UpdatedAtUtc { get; set; }
}
