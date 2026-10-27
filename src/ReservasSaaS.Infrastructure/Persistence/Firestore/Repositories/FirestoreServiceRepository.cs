using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Domain.ValueObjects;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Abstractions;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Documents;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Errors;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Interfaces;

namespace ReservasSaaS.Infrastructure.Persistence.Firestore.Repositories;

public class FirestoreServiceRepository : FirestoreRepository<Service, ServiceDocument, Guid>, IServiceRepository
{
    public override string CollectionName => "services";

    public FirestoreServiceRepository(
        IFirestoreProvider firestoreProvider,
        ILogger<FirestoreServiceRepository> logger)
        : base(firestoreProvider, logger)
    {
    }

    protected override string ToDocumentId(Guid id) => id.ToString("N");

    protected override ServiceDocument ToDocument(Service entity) => new()
    {
        Id = ToDocumentId(entity.Id),
        CompanyId = entity.CompanyId.ToString("N"),
        Name = entity.Name,
        Description = entity.Description,
        DurationMinutes = entity.DurationMinutes,
        BufferBeforeMinutes = entity.BufferBeforeMinutes,
        BufferAfterMinutes = entity.BufferAfterMinutes,
        Price = (double)entity.Price,
        Currency = entity.Currency,
        Status = (int)entity.Status,
        RequiresDeposit = entity.DepositPolicy.RequiresDeposit,
        DepositType = (int)entity.DepositPolicy.Type,
        DepositValue = (double)entity.DepositPolicy.Value,
        DepositDescription = entity.DepositPolicy.Description,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc
    };

    protected override Service ToEntity(ServiceDocument document)
    {
        var id = Guid.Parse(document.Id);
        var companyId = Guid.Parse(document.CompanyId);

        var depositPolicy = document.RequiresDeposit
            ? new DepositPolicy(true, (DepositType)document.DepositType, (decimal)document.DepositValue, document.DepositDescription)
            : DepositPolicy.None;

        var service = new Service(
            id: id,
            companyId: companyId,
            name: document.Name,
            description: document.Description,
            durationMinutes: document.DurationMinutes,
            price: (decimal)document.Price,
            currency: document.Currency,
            bufferBeforeMinutes: document.BufferBeforeMinutes,
            bufferAfterMinutes: document.BufferAfterMinutes,
            depositPolicy: depositPolicy,
            status: (ServiceStatus)document.Status
        );

        return service;
    }

    public async Task<IReadOnlyList<Service>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var compStr = companyId.ToString("N");
        try
        {
            var query = GetCollection().WhereEqualTo(nameof(ServiceDocument.CompanyId), compStr);
            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            var list = new List<Service>(snapshot.Count);

            foreach (var docSnapshot in snapshot.Documents)
            {
                var doc = docSnapshot.ConvertTo<ServiceDocument>();
                doc.Id = docSnapshot.Id;
                list.Add(ToEntity(doc));
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query services for Company '{CompanyId}'", compStr);
            throw FirestoreErrorMapper.Map(ex, nameof(Service));
        }
    }

    public async Task<IReadOnlyList<Service>> GetActiveByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var compStr = companyId.ToString("N");
        try
        {
            var query = GetCollection()
                .WhereEqualTo(nameof(ServiceDocument.CompanyId), compStr)
                .WhereEqualTo(nameof(ServiceDocument.Status), (int)ServiceStatus.Active);

            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            var list = new List<Service>(snapshot.Count);

            foreach (var docSnapshot in snapshot.Documents)
            {
                var doc = docSnapshot.ConvertTo<ServiceDocument>();
                doc.Id = docSnapshot.Id;
                list.Add(ToEntity(doc));
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query active services for Company '{CompanyId}'", compStr);
            throw FirestoreErrorMapper.Map(ex, nameof(Service));
        }
    }

    public async Task<Service?> GetByIdAsync(string companyId, string serviceId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(companyId, out var companyGuid) ||
         !Guid.TryParse(serviceId, out var serviceGuid))
        {
            return null;
        }

        var service =  await GetByIdAsync(serviceGuid, cancellationToken);

        if (service is null || service.CompanyId != companyGuid)
        {
            return null;
        }

        return service;
    }
}
