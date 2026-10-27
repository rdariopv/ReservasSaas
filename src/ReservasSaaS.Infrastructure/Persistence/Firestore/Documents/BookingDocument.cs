using Google.Cloud.Firestore;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Abstractions;

namespace ReservasSaaS.Infrastructure.Persistence.Firestore.Documents;

[FirestoreData]
public class BookingDocument : IFirestoreDocument
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty]
    public string CompanyId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string ServiceId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string ServiceName { get; set; } = string.Empty;

    [FirestoreProperty]
    public string? ProfessionalId { get; set; }

    [FirestoreProperty]
    public string? ProfessionalName { get; set; }

    [FirestoreProperty]
    public string CustomerName { get; set; } = string.Empty;

    [FirestoreProperty]
    public string CustomerEmail { get; set; } = string.Empty;

    [FirestoreProperty]
    public string CustomerPhone { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime ScheduledStartTimeUtc { get; set; }

    [FirestoreProperty]
    public DateTime ScheduledEndTimeUtc { get; set; }

    [FirestoreProperty]
    public int Status { get; set; } = (int)BookingStatus.PendingAssignment;

    [FirestoreProperty]
    public int PaymentStatus { get; set; } = (int)Domain.Enums.PaymentStatus.NotRequired;

    [FirestoreProperty]
    public double TotalServicePrice { get; set; }

    [FirestoreProperty]
    public double DepositRequiredAmount { get; set; }

    [FirestoreProperty]
    public double DepositPaidAmount { get; set; }

    [FirestoreProperty]
    public string Currency { get; set; } = "COP";

    [FirestoreProperty]
    public DateTime? ExpiresAtUtc { get; set; }

    [FirestoreProperty]
    public string? CancellationReason { get; set; }

    [FirestoreProperty]
    public string? RejectionReason { get; set; }

    [FirestoreProperty]
    public string? Notes { get; set; }

    [FirestoreProperty]
    public string? IdempotencyKey { get; set; }

    [FirestoreProperty]
    public int Version { get; set; } = 1;

    [FirestoreProperty]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [FirestoreProperty]
    public DateTime? UpdatedAtUtc { get; set; }
}
