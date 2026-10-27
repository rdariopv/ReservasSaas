using Google.Cloud.Firestore;

namespace ReservasSaaS.Infrastructure.Persistence.Firestore.Interfaces;

/// <summary>
/// Provides access to the managed FirestoreDb instance and handles connectivity diagnostics.
/// </summary>
public interface IFirestoreProvider
{
    FirestoreDb GetDatabase();
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
    string ProjectId { get; }
    string DatabaseId { get; }
    bool IsEmulator { get; }
}
