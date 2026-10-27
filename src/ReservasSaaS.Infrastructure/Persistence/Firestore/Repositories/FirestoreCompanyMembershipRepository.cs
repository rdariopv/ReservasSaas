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

public class FirestoreCompanyMembershipRepository : FirestoreRepository<CompanyMembership, CompanyMembershipDocument, Guid>, ICompanyMembershipRepository
{
    public override string CollectionName => "memberships";

    public FirestoreCompanyMembershipRepository(
        IFirestoreProvider firestoreProvider,
        ILogger<FirestoreCompanyMembershipRepository> logger)
        : base(firestoreProvider, logger)
    {
    }

    protected override string ToDocumentId(Guid id) => id.ToString("N");

    protected override CompanyMembershipDocument ToDocument(CompanyMembership entity) => new()
    {
        Id = ToDocumentId(entity.Id),
        CompanyId = entity.CompanyId.ToString("N"),
        UserId = entity.UserId.ToString("N"),
        Role = (int)entity.Role,
        Status = (int)entity.Status,
        CustomPermissions = entity.CustomPermissions.ToList(),
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc
    };

    protected override CompanyMembership ToEntity(CompanyMembershipDocument document)
    {
        var id = Guid.Parse(document.Id);
        var companyId = Guid.Parse(document.CompanyId);
        var userId = Guid.Parse(document.UserId);

        var membership = new CompanyMembership(
            id: id,
            companyId: companyId,
            userId: userId,
            role: (CompanyRole)document.Role,
            status: (MembershipStatus)document.Status,
            customPermissions: document.CustomPermissions
        );

        return membership;
    }

    public async Task<CompanyMembership?> GetMembershipAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var companyStr = companyId.ToString("N");
        var userStr = userId.ToString("N");

        try
        {
            var query = GetCollection()
                .WhereEqualTo(nameof(CompanyMembershipDocument.CompanyId), companyStr)
                .WhereEqualTo(nameof(CompanyMembershipDocument.UserId), userStr)
                .Limit(1);

            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            if (snapshot.Count == 0) return null;

            var doc = snapshot.Documents[0].ConvertTo<CompanyMembershipDocument>();
            doc.Id = snapshot.Documents[0].Id;
            return ToEntity(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query membership for Company '{CompanyId}' and User '{UserId}'", companyStr, userStr);
            throw FirestoreErrorMapper.Map(ex, nameof(CompanyMembership));
        }
    }

    public async Task<IReadOnlyList<CompanyMembership>> GetUserMembershipsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userStr = userId.ToString("N");

        try
        {
            var query = GetCollection().WhereEqualTo(nameof(CompanyMembershipDocument.UserId), userStr);
            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            var list = new List<CompanyMembership>(snapshot.Count);

            foreach (var docSnapshot in snapshot.Documents)
            {
                var doc = docSnapshot.ConvertTo<CompanyMembershipDocument>();
                doc.Id = docSnapshot.Id;
                list.Add(ToEntity(doc));
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query memberships for User '{UserId}'", userStr);
            throw FirestoreErrorMapper.Map(ex, nameof(CompanyMembership));
        }
    }

    public async Task<IReadOnlyList<CompanyMembership>> GetCompanyMembersAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var companyStr = companyId.ToString("N");

        try
        {
            var query = GetCollection().WhereEqualTo(nameof(CompanyMembershipDocument.CompanyId), companyStr);
            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            var list = new List<CompanyMembership>(snapshot.Count);

            foreach (var docSnapshot in snapshot.Documents)
            {
                var doc = docSnapshot.ConvertTo<CompanyMembershipDocument>();
                doc.Id = docSnapshot.Id;
                list.Add(ToEntity(doc));
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query members for Company '{CompanyId}'", companyStr);
            throw FirestoreErrorMapper.Map(ex, nameof(CompanyMembership));
        }
    }
}
