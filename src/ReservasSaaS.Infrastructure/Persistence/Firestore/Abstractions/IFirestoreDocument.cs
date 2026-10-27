namespace ReservasSaaS.Infrastructure.Persistence.Firestore.Abstractions;

/// <summary>
/// Contract for Firestore documents with audit timestamps and string document keys.
/// </summary>
public interface IFirestoreDocument
{
    string Id { get; set; }
    DateTime CreatedAtUtc { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
}
