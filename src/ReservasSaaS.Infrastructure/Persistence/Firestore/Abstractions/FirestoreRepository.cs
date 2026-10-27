using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using ReservasSaaS.Domain.Common;
using ReservasSaaS.Domain.Ports;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Errors;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Interfaces;

namespace ReservasSaaS.Infrastructure.Persistence.Firestore.Abstractions;

/// <summary>
/// Generic repository adapter for Cloud Firestore implementing Domain IRepository port.
/// Handles domain entity to/from Firestore document transformation, asynchronous operations,
/// and centralized gRPC/Firestore error translation.
/// </summary>
/// <typeparam name="TEntity">Domain Aggregate Root entity type</typeparam>
/// <typeparam name="TDocument">Firestore document POCO with [FirestoreData] attributes</typeparam>
/// <typeparam name="TId">Entity identifier type (e.g. Guid or string)</typeparam>
public abstract class FirestoreRepository<TEntity, TDocument, TId> : IRepository<TEntity, TId>
    where TEntity : BaseEntity<TId>, IAggregateRoot
    where TDocument : class, IFirestoreDocument, new()
{
    protected readonly IFirestoreProvider _firestoreProvider;
    protected readonly ILogger _logger;

    public abstract string CollectionName { get; }

    protected FirestoreRepository(
        IFirestoreProvider firestoreProvider,
        ILogger logger)
    {
        _firestoreProvider = firestoreProvider;
        _logger = logger;
    }

    protected CollectionReference GetCollection()
    {
        return _firestoreProvider.GetDatabase().Collection(CollectionName);
    }

    protected abstract string ToDocumentId(TId id);
    protected abstract TDocument ToDocument(TEntity entity);
    protected abstract TEntity ToEntity(TDocument document);

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var docId = ToDocumentId(id);
        var docRef = GetCollection().Document(docId);

        try
        {
            var snapshot = await docRef.GetSnapshotAsync(cancellationToken);
            if (!snapshot.Exists)
            {
                return null;
            }

            var doc = snapshot.ConvertTo<TDocument>();
            doc.Id = snapshot.Id;
            return ToEntity(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve entity '{Entity}' with Id '{Id}' from collection '{Collection}'",
                typeof(TEntity).Name, docId, CollectionName);
            throw FirestoreErrorMapper.Map(ex, typeof(TEntity).Name, docId);
        }
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await GetCollection().GetSnapshotAsync(cancellationToken);
            var results = new List<TEntity>(snapshot.Count);

            foreach (var docSnapshot in snapshot.Documents)
            {
                var doc = docSnapshot.ConvertTo<TDocument>();
                doc.Id = docSnapshot.Id;
                results.Add(ToEntity(doc));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all entities for collection '{Collection}'", CollectionName);
            throw FirestoreErrorMapper.Map(ex, typeof(TEntity).Name);
        }
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var docId = ToDocumentId(entity.Id);
        var docRef = GetCollection().Document(docId);
        var document = ToDocument(entity);
        document.Id = docId;

        try
        {
            // Set with precondition Create to fail if document already exists
            await docRef.CreateAsync(document, cancellationToken);
            _logger.LogInformation("Entity '{Entity}' with Id '{Id}' created in collection '{Collection}'",
                typeof(TEntity).Name, docId, CollectionName);

            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add entity '{Entity}' with Id '{Id}' to collection '{Collection}'",
                typeof(TEntity).Name, docId, CollectionName);
            throw FirestoreErrorMapper.Map(ex, typeof(TEntity).Name, docId);
        }
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var docId = ToDocumentId(entity.Id);
        var docRef = GetCollection().Document(docId);
        var document = ToDocument(entity);
        document.Id = docId;

        try
        {
            // SetAsync without merge overwrites the document with the updated state
            await docRef.SetAsync(document, SetOptions.Overwrite, cancellationToken);
            _logger.LogInformation("Entity '{Entity}' with Id '{Id}' updated in collection '{Collection}'",
                typeof(TEntity).Name, docId, CollectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update entity '{Entity}' with Id '{Id}' in collection '{Collection}'",
                typeof(TEntity).Name, docId, CollectionName);
            throw FirestoreErrorMapper.Map(ex, typeof(TEntity).Name, docId);
        }
    }

    public virtual async Task DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        var docId = ToDocumentId(id);
        var docRef = GetCollection().Document(docId);

        try
        {
            await docRef.DeleteAsync(null, cancellationToken);
            _logger.LogInformation("Entity '{Entity}' with Id '{Id}' deleted from collection '{Collection}'",
                typeof(TEntity).Name, docId, CollectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete entity '{Entity}' with Id '{Id}' from collection '{Collection}'",
                typeof(TEntity).Name, docId, CollectionName);
            throw FirestoreErrorMapper.Map(ex, typeof(TEntity).Name, docId);
        }
    }

    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        var docId = ToDocumentId(id);
        var docRef = GetCollection().Document(docId);

        try
        {
            var snapshot = await docRef.GetSnapshotAsync(cancellationToken);
            return snapshot.Exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence for entity '{Entity}' with Id '{Id}' in '{Collection}'",
                typeof(TEntity).Name, docId, CollectionName);
            throw FirestoreErrorMapper.Map(ex, typeof(TEntity).Name, docId);
        }
    }
}
