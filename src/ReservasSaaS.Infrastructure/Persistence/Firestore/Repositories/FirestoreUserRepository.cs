using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Abstractions;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Documents;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Errors;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Interfaces;

namespace ReservasSaaS.Infrastructure.Persistence.Firestore.Repositories;

public class FirestoreUserRepository : FirestoreRepository<User, UserDocument, Guid>, IUserRepository
{
    public override string CollectionName => "users";

    public FirestoreUserRepository(
        IFirestoreProvider firestoreProvider,
        ILogger<FirestoreUserRepository> logger)
        : base(firestoreProvider, logger)
    {
    }

    protected override string ToDocumentId(Guid id) => id.ToString("N");

    protected override UserDocument ToDocument(User entity) => new()
    {
        Id = ToDocumentId(entity.Id),
        FirebaseUid = entity.FirebaseUid,
        Email = entity.Email,
        DisplayName = entity.DisplayName,
        PhoneNumber = entity.PhoneNumber,
        PhotoUrl = entity.PhotoUrl,
        IsSaaSAdministrator = entity.IsSaaSAdministrator,
        IsActive = entity.IsActive,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc
    };

    protected override User ToEntity(UserDocument document)
    {
        var id = Guid.Parse(document.Id);
        var user = new User(
            id: id,
            firebaseUid: document.FirebaseUid,
            email: document.Email,
            displayName: document.DisplayName,
            phoneNumber: document.PhoneNumber,
            photoUrl: document.PhotoUrl,
            isSaaSAdministrator: document.IsSaaSAdministrator
        );

        if (!document.IsActive)
        {
            user.Deactivate();
        }

        return user;
    }

    public async Task<User?> GetByFirebaseUidAsync(string firebaseUid, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = GetCollection().WhereEqualTo(nameof(UserDocument.FirebaseUid), firebaseUid).Limit(1);
            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            if (snapshot.Count == 0) return null;

            var doc = snapshot.Documents[0].ConvertTo<UserDocument>();
            doc.Id = snapshot.Documents[0].Id;
            return ToEntity(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query user by FirebaseUid '{FirebaseUid}'", firebaseUid);
            throw FirestoreErrorMapper.Map(ex, nameof(User));
        }
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = GetCollection().WhereEqualTo(nameof(UserDocument.Email), email.Trim().ToLowerInvariant()).Limit(1);
            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            if (snapshot.Count == 0) return null;

            var doc = snapshot.Documents[0].ConvertTo<UserDocument>();
            doc.Id = snapshot.Documents[0].Id;
            return ToEntity(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query user by Email '{Email}'", email);
            throw FirestoreErrorMapper.Map(ex, nameof(User));
        }
    }
}
