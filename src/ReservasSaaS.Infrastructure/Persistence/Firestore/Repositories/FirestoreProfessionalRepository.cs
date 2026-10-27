using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Domain.ValueObjects;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Abstractions;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Documents;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Errors;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Interfaces;

namespace ReservasSaaS.Infrastructure.Persistence.Firestore.Repositories;

public class FirestoreProfessionalRepository : FirestoreRepository<Professional, ProfessionalDocument, Guid>, IProfessionalRepository
{
    public override string CollectionName => "professionals";

    public FirestoreProfessionalRepository(
        IFirestoreProvider firestoreProvider,
        ILogger<FirestoreProfessionalRepository> logger)
        : base(firestoreProvider, logger)
    {
    }

    protected override string ToDocumentId(Guid id) => id.ToString("N");

    protected override ProfessionalDocument ToDocument(Professional entity) => new()
    {
        Id = ToDocumentId(entity.Id),
        CompanyId = entity.CompanyId.ToString("N"),
        UserId = entity.UserId?.ToString("N"),
        Name = entity.Name,
        Email = entity.Email,
        PhoneNumber = entity.PhoneNumber,
        Specialty = entity.Specialty,
        IsActive = entity.IsActive,
        AssignedServiceIds = entity.AssignedServiceIds.Select(id => id.ToString("N")).ToList(),
        WorkingSchedules = entity.WorkingSchedules.Select(w => new WorkingScheduleItem
        {
            DayOfWeek = w.DayOfWeek.ToString(),
            StartTime = w.StartTime.ToString("HH:mm"),
            EndTime = w.EndTime.ToString("HH:mm"),
            IsEnabled = w.IsEnabled
        }).ToList(),
        BreakSchedules = entity.BreakSchedules.Select(b => new BreakScheduleItem
        {
            DayOfWeek = b.DayOfWeek.ToString(),
            StartTime = b.StartTime.ToString("HH:mm"),
            EndTime = b.EndTime.ToString("HH:mm"),
            Name = b.Name
        }).ToList(),
        Exceptions = entity.Exceptions.Select(e => new ScheduleExceptionItem
        {
            Date = e.Date.ToString("yyyy-MM-dd"),
            IsDayOff = e.IsDayOff,
            StartTime = e.StartTime?.ToString("HH:mm"),
            EndTime = e.EndTime?.ToString("HH:mm"),
            Reason = e.Reason
        }).ToList(),
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc
    };

    protected override Professional ToEntity(ProfessionalDocument document)
    {
        var id = Guid.Parse(document.Id);
        var companyId = Guid.Parse(document.CompanyId);
        Guid? userId = !string.IsNullOrWhiteSpace(document.UserId) ? Guid.Parse(document.UserId) : null;

        var assignedServices = document.AssignedServiceIds
            .Where(s => Guid.TryParse(s, out _))
            .Select(Guid.Parse);

        var workingSchedules = document.WorkingSchedules.Select(w =>
        {
            var day = Enum.Parse<DayOfWeek>(w.DayOfWeek, ignoreCase: true);
            var start = TimeOnly.Parse(w.StartTime);
            var end = TimeOnly.Parse(w.EndTime);
            return new WorkingSchedule(day, start, end, w.IsEnabled);
        });

        var breakSchedules = document.BreakSchedules.Select(b =>
        {
            var day = Enum.Parse<DayOfWeek>(b.DayOfWeek, ignoreCase: true);
            var start = TimeOnly.Parse(b.StartTime);
            var end = TimeOnly.Parse(b.EndTime);
            return new BreakSchedule(day, start, end, b.Name);
        });

        var exceptions = document.Exceptions.Select(e =>
        {
            var date = DateOnly.Parse(e.Date);
            TimeOnly? start = !string.IsNullOrWhiteSpace(e.StartTime) ? TimeOnly.Parse(e.StartTime) : null;
            TimeOnly? end = !string.IsNullOrWhiteSpace(e.EndTime) ? TimeOnly.Parse(e.EndTime) : null;
            return new ScheduleException(date, e.IsDayOff, start, end, e.Reason);
        });

        var professional = new Professional(
            id: id,
            companyId: companyId,
            name: document.Name,
            userId: userId,
            email: document.Email,
            phoneNumber: document.PhoneNumber,
            specialty: document.Specialty,
            isActive: document.IsActive,
            assignedServiceIds: assignedServices,
            workingSchedules: workingSchedules,
            breakSchedules: breakSchedules
        );

        foreach (var ex in exceptions)
        {
            professional.AddException(ex);
        }

        return professional;
    }

    public async Task<IReadOnlyList<Professional>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var compStr = companyId.ToString("N");
        try
        {
            var query = GetCollection().WhereEqualTo(nameof(ProfessionalDocument.CompanyId), compStr);
            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            var list = new List<Professional>(snapshot.Count);

            foreach (var docSnapshot in snapshot.Documents)
            {
                var doc = docSnapshot.ConvertTo<ProfessionalDocument>();
                doc.Id = docSnapshot.Id;
                list.Add(ToEntity(doc));
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query professionals for Company '{CompanyId}'", compStr);
            throw FirestoreErrorMapper.Map(ex, nameof(Professional));
        }
    }

    public async Task<IReadOnlyList<Professional>> GetByServiceIdAsync(Guid companyId, Guid serviceId, CancellationToken cancellationToken = default)
    {
        var compStr = companyId.ToString("N");
        var servStr = serviceId.ToString("N");

        try
        {
            var query = GetCollection()
                .WhereEqualTo(nameof(ProfessionalDocument.CompanyId), compStr)
                .WhereArrayContains(nameof(ProfessionalDocument.AssignedServiceIds), servStr);

            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            var list = new List<Professional>(snapshot.Count);

            foreach (var docSnapshot in snapshot.Documents)
            {
                var doc = docSnapshot.ConvertTo<ProfessionalDocument>();
                doc.Id = docSnapshot.Id;
                list.Add(ToEntity(doc));
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query professionals for Service '{ServiceId}' in Company '{CompanyId}'", servStr, compStr);
            throw FirestoreErrorMapper.Map(ex, nameof(Professional));
        }
    }

    public async Task<Professional?> GetByUserIdAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var compStr = companyId.ToString("N");
        var userStr = userId.ToString("N");

        try
        {
            var query = GetCollection()
                .WhereEqualTo(nameof(ProfessionalDocument.CompanyId), compStr)
                .WhereEqualTo(nameof(ProfessionalDocument.UserId), userStr)
                .Limit(1);

            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            if (snapshot.Count == 0) return null;

            var doc = snapshot.Documents[0].ConvertTo<ProfessionalDocument>();
            doc.Id = snapshot.Documents[0].Id;
            return ToEntity(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query professional by UserId '{UserId}' in Company '{CompanyId}'", userStr, compStr);
            throw FirestoreErrorMapper.Map(ex, nameof(Professional));
        }
    }
}
