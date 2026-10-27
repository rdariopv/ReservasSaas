using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Abstractions;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Documents;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Errors;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Interfaces;

namespace ReservasSaaS.Infrastructure.Persistence.Firestore.Repositories;

public class FirestoreCompanyRepository : FirestoreRepository<Company, CompanyDocument, Guid>, ICompanyRepository
{
    public override string CollectionName => "companies";

    public FirestoreCompanyRepository(
        IFirestoreProvider firestoreProvider,
        ILogger<FirestoreCompanyRepository> logger)
        : base(firestoreProvider, logger)
    {
    }

    protected override string ToDocumentId(Guid id) => id.ToString("N");

    protected override CompanyDocument ToDocument(Company entity) => new()
    {
        Id = ToDocumentId(entity.Id),
        Name = entity.Name,
        Slug = entity.Slug,
        TaxId = entity.TaxId,
        TimeZone = entity.TimeZone,
        Currency = entity.Currency,
        Status = (int)entity.Status,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc
    };

    protected override Company ToEntity(CompanyDocument document)
    {
        var id = Guid.Parse(document.Id);
        var company = new Company(
            id: id,
            name: document.Name,
            slug: document.Slug,
            taxId: document.TaxId,
            timeZone: document.TimeZone ?? "America/Bogota",
            currency: document.Currency ?? "COP"
        );

        if (document.Status != (int)CompanyStatus.Active)
        {
            company.ChangeStatus((CompanyStatus)document.Status);
        }

        return company;
    }

    public async Task<Company?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = GetCollection().WhereEqualTo(nameof(CompanyDocument.Slug), slug.Trim().ToLowerInvariant()).Limit(1);
            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            if (snapshot.Count == 0) return null;

            var doc = snapshot.Documents[0].ConvertTo<CompanyDocument>();
            doc.Id = snapshot.Documents[0].Id;
            return ToEntity(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query company by slug '{Slug}'", slug);
            throw FirestoreErrorMapper.Map(ex, nameof(Company));
        }
    }
}
