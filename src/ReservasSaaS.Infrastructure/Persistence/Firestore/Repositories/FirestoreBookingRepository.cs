using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Abstractions;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Documents;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Errors;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Interfaces;

namespace ReservasSaaS.Infrastructure.Persistence.Firestore.Repositories;

public class FirestoreBookingRepository : FirestoreRepository<Booking, BookingDocument, Guid>, IBookingRepository
{
    public override string CollectionName => "bookings";

    public FirestoreBookingRepository(
        IFirestoreProvider firestoreProvider,
        ILogger<FirestoreBookingRepository> logger)
        : base(firestoreProvider, logger)
    {
    }

    protected override string ToDocumentId(Guid id) => id.ToString("N");

    protected override BookingDocument ToDocument(Booking entity) => new()
    {
        Id = ToDocumentId(entity.Id),
        CompanyId = entity.CompanyId.ToString("N"),
        ServiceId = entity.ServiceId.ToString("N"),
        ServiceName = entity.ServiceName,
        ProfessionalId = entity.ProfessionalId?.ToString("N"),
        ProfessionalName = entity.ProfessionalName,
        CustomerName = entity.CustomerName,
        CustomerEmail = entity.CustomerEmail,
        CustomerPhone = entity.CustomerPhone,
        ScheduledStartTimeUtc = entity.ScheduledStartTimeUtc,
        ScheduledEndTimeUtc = entity.ScheduledEndTimeUtc,
        Status = (int)entity.Status,
        PaymentStatus = (int)entity.PaymentStatus,
        TotalServicePrice = (double)entity.TotalServicePrice,
        DepositRequiredAmount = (double)entity.DepositRequiredAmount,
        DepositPaidAmount = (double)entity.DepositPaidAmount,
        Currency = entity.Currency,
        ExpiresAtUtc = entity.ExpiresAtUtc,
        CancellationReason = entity.CancellationReason,
        RejectionReason = entity.RejectionReason,
        Notes = entity.Notes,
        IdempotencyKey = entity.IdempotencyKey,
        Version = entity.Version,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc
    };

    protected override Booking ToEntity(BookingDocument doc)
    {
        var id = Guid.Parse(doc.Id);
        var companyId = Guid.Parse(doc.CompanyId);
        var serviceId = Guid.Parse(doc.ServiceId);
        Guid? professionalId = !string.IsNullOrWhiteSpace(doc.ProfessionalId)
            ? Guid.Parse(doc.ProfessionalId)
            : null;

        return Booking.Reconstitute(
            id: id,
            companyId: companyId,
            serviceId: serviceId,
            serviceName: doc.ServiceName,
            professionalId: professionalId,
            professionalName: doc.ProfessionalName,
            customerName: doc.CustomerName,
            customerEmail: doc.CustomerEmail,
            customerPhone: doc.CustomerPhone,
            scheduledStartTimeUtc: doc.ScheduledStartTimeUtc,
            scheduledEndTimeUtc: doc.ScheduledEndTimeUtc,
            status: (BookingStatus)doc.Status,
            paymentStatus: (PaymentStatus)doc.PaymentStatus,
            totalServicePrice: (decimal)doc.TotalServicePrice,
            depositRequiredAmount: (decimal)doc.DepositRequiredAmount,
            depositPaidAmount: (decimal)doc.DepositPaidAmount,
            currency: doc.Currency,
            expiresAtUtc: doc.ExpiresAtUtc,
            cancellationReason: doc.CancellationReason,
            rejectionReason: doc.RejectionReason,
            notes: doc.Notes,
            idempotencyKey: doc.IdempotencyKey,
            version: doc.Version,
            createdAtUtc: doc.CreatedAtUtc,
            updatedAtUtc: doc.UpdatedAtUtc);
    }

    public async Task<Booking?> GetByIdAsync(Guid companyId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        var doc = await GetByIdAsync(bookingId, cancellationToken);
        if (doc != null && doc.CompanyId == companyId)
        {
            return doc;
        }

        return null;
    }

    public async Task<Booking?> GetByIdempotencyKeyAsync(
        Guid companyId, 
        string idempotencyKey, 
        CancellationToken cancellationToken = default)
    {
        var compStr = companyId.ToString("N");
        try
        {
            var query = GetCollection()
                .WhereEqualTo(nameof(BookingDocument.CompanyId), compStr)
                .WhereEqualTo(nameof(BookingDocument.IdempotencyKey), idempotencyKey)
                .Limit(1);

            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            if (snapshot.Count == 0) return null;

            var doc = snapshot.Documents[0].ConvertTo<BookingDocument>();
            doc.Id = snapshot.Documents[0].Id;
            return ToEntity(doc);
        }
        catch (Exception ex)
        {
            throw FirestoreErrorMapper.Map(ex, "GetByIdempotencyKey", CollectionName, idempotencyKey);
        }
    }

    public async Task<IReadOnlyList<Booking>> GetOverlappingBookingsAsync(
        Guid companyId,
        Guid professionalId,
        DateTime startUtc,
        DateTime endUtc,
        Guid? excludeBookingId = null,
        CancellationToken cancellationToken = default)
    {
        var compStr = companyId.ToString("N");
        var profStr = professionalId.ToString("N");

        try
        {
            // Query bookings of company and professional
            var query = GetCollection()
                .WhereEqualTo(nameof(BookingDocument.CompanyId), compStr)
                .WhereEqualTo(nameof(BookingDocument.ProfessionalId), profStr);

            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            var results = new List<Booking>();

            var inactiveStatuses = new HashSet<int>
            {
                (int)BookingStatus.Cancelled,
                (int)BookingStatus.Expired,
                (int)BookingStatus.Rejected,
                (int)BookingStatus.NoShow
            };

            foreach (var docSnapshot in snapshot.Documents)
            {
                if (excludeBookingId.HasValue && docSnapshot.Id == excludeBookingId.Value.ToString("N"))
                    continue;

                var doc = docSnapshot.ConvertTo<BookingDocument>();
                doc.Id = docSnapshot.Id;

                if (inactiveStatuses.Contains(doc.Status))
                    continue;

                // Overlap condition: startA < endB && endA > startB
                if (doc.ScheduledStartTimeUtc < endUtc && doc.ScheduledEndTimeUtc > startUtc)
                {
                    results.Add(ToEntity(doc));
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            throw FirestoreErrorMapper.Map(ex, "GetOverlappingBookings", CollectionName, profStr);
        }
    }

    public async Task<IReadOnlyList<Booking>> GetActiveBookingsInIntervalAsync(
        Guid companyId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        var compStr = companyId.ToString("N");

        try
        {
            var query = GetCollection()
                .WhereEqualTo(nameof(BookingDocument.CompanyId), compStr);

            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            var results = new List<Booking>();

            var inactiveStatuses = new HashSet<int>
            {
                (int)BookingStatus.Cancelled,
                (int)BookingStatus.Expired,
                (int)BookingStatus.Rejected,
                (int)BookingStatus.NoShow
            };

            foreach (var docSnapshot in snapshot.Documents)
            {
                var doc = docSnapshot.ConvertTo<BookingDocument>();
                doc.Id = docSnapshot.Id;

                if (inactiveStatuses.Contains(doc.Status))
                    continue;

                if (doc.ScheduledStartTimeUtc < endUtc && doc.ScheduledEndTimeUtc > startUtc)
                {
                    results.Add(ToEntity(doc));
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            throw FirestoreErrorMapper.Map(ex, "GetActiveBookingsInInterval", CollectionName, compStr);
        }
    }

    public async Task<IReadOnlyList<Booking>> GetStalePendingBookingsAsync(
        Guid companyId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var compStr = companyId.ToString("N");

        try
        {
            var query = GetCollection()
                .WhereEqualTo(nameof(BookingDocument.CompanyId), compStr);

            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            var results = new List<Booking>();

            var pendingStatuses = new HashSet<int>
            {
                (int)BookingStatus.PendingAssignment,
                (int)BookingStatus.PendingPayment,
                (int)BookingStatus.PaymentReview
            };

            foreach (var docSnapshot in snapshot.Documents)
            {
                var doc = docSnapshot.ConvertTo<BookingDocument>();
                doc.Id = docSnapshot.Id;

                if (pendingStatuses.Contains(doc.Status) && 
                    doc.ExpiresAtUtc.HasValue && 
                    doc.ExpiresAtUtc.Value <= utcNow)
                {
                    results.Add(ToEntity(doc));
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            throw FirestoreErrorMapper.Map(ex, "GetStalePendingBookings", CollectionName, compStr);
        }
    }

    public async Task<(IReadOnlyList<Booking> Items, int TotalCount)> SearchAsync(
        Guid companyId,
        BookingStatus? status = null,
        Guid? professionalId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int page = 1,
        int pageSize = 20,
        PaymentStatus? paymentStatus = null,
        CancellationToken cancellationToken = default)
    {
        var compStr = companyId.ToString("N");

        try
        {
            var query = GetCollection()
                .WhereEqualTo(nameof(BookingDocument.CompanyId), compStr);

            if (status.HasValue)
            {
                query = query.WhereEqualTo(nameof(BookingDocument.Status), (int)status.Value);
            }

            if (professionalId.HasValue)
            {
                query = query.WhereEqualTo(nameof(BookingDocument.ProfessionalId), professionalId.Value.ToString("N"));
            }

            var snapshot = await query.GetSnapshotAsync(cancellationToken);
            var all = new List<Booking>();

            foreach (var docSnapshot in snapshot.Documents)
            {
                var doc = docSnapshot.ConvertTo<BookingDocument>();
                doc.Id = docSnapshot.Id;

                if (paymentStatus.HasValue && doc.PaymentStatus != (int)paymentStatus.Value)
                    continue;

                if (fromUtc.HasValue && doc.ScheduledStartTimeUtc < fromUtc.Value)
                    continue;

                if (toUtc.HasValue && doc.ScheduledStartTimeUtc > toUtc.Value)
                    continue;

                all.Add(ToEntity(doc));
            }

            // Order by start time descending
            all = all.OrderByDescending(b => b.ScheduledStartTimeUtc).ToList();
            var totalCount = all.Count;

            var paged = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (paged, totalCount);
        }
        catch (Exception ex)
        {
            throw FirestoreErrorMapper.Map(ex, "SearchAsync", CollectionName, compStr);
        }
    }

    public  async Task CreateAsync(Booking entity, CancellationToken cancellationToken = default)
    {
        // Enforce concurrency validation: verify no duplicate ID exists
        var docId = ToDocumentId(entity.Id);
        var docRef = GetCollection().Document(docId);

       // var docRef =  GetDocumentReference(entity.Id);
        var snapshot = await docRef.GetSnapshotAsync(cancellationToken);
        if (snapshot.Exists)
        {
            throw new ConcurrencyException($"A booking with ID '{entity.Id}' already exists in persistence.");
        }

        var document = ToDocument(entity);
        await docRef.SetAsync(document, cancellationToken: cancellationToken);
    }
}
