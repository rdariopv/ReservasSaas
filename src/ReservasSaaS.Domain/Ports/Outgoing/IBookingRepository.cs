using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;

namespace ReservasSaaS.Domain.Ports.Outgoing;

/// <summary>
/// Secondary port for Booking persistence, tenant-isolated queries, concurrency control,
/// and schedule conflict checks.
/// </summary>
public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid companyId, Guid bookingId, CancellationToken cancellationToken = default);

    Task<Booking?> GetByIdempotencyKeyAsync(Guid companyId, string idempotencyKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetOverlappingBookingsAsync(
        Guid companyId, 
        Guid professionalId, 
        DateTime startUtc, 
        DateTime endUtc, 
        Guid? excludeBookingId = null, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetActiveBookingsInIntervalAsync(
        Guid companyId, 
        DateTime startUtc, 
        DateTime endUtc, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetStalePendingBookingsAsync(
        Guid companyId, 
        DateTime utcNow, 
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Booking> Items, int TotalCount)> SearchAsync(
        Guid companyId,
        BookingStatus? status = null,
        Guid? professionalId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int page = 1,
        int pageSize = 20,
        PaymentStatus? paymentStatus = null,
        CancellationToken cancellationToken = default);

    Task CreateAsync(Booking booking, CancellationToken cancellationToken = default);

    Task UpdateAsync(Booking booking, CancellationToken cancellationToken = default);
}
