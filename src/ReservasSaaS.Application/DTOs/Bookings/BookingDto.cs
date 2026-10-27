using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;

namespace ReservasSaaS.Application.DTOs.Bookings;

/// <summary>
/// Data Transfer Object representing a booking and its complete status.
/// </summary>
public record BookingDto(
    Guid Id,
    Guid CompanyId,
    Guid ServiceId,
    string ServiceName,
    Guid? ProfessionalId,
    string? ProfessionalName,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    DateTime ScheduledStartTimeUtc,
    DateTime ScheduledEndTimeUtc,
    string Status,
    string PaymentStatus,
    decimal TotalServicePrice,
    decimal DepositRequiredAmount,
    decimal DepositPaidAmount,
    string Currency,
    DateTime? ExpiresAtUtc,
    string? CancellationReason,
    string? RejectionReason,
    string? Notes,
    string? IdempotencyKey,
    int Version,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc)
{
    public static BookingDto FromDomain(Booking booking) => new(
        booking.Id,
        booking.CompanyId,
        booking.ServiceId,
        booking.ServiceName,
        booking.ProfessionalId,
        booking.ProfessionalName,
        booking.CustomerName,
        booking.CustomerEmail,
        booking.CustomerPhone,
        booking.ScheduledStartTimeUtc,
        booking.ScheduledEndTimeUtc,
        booking.Status.ToString(),
        booking.PaymentStatus.ToString(),
        booking.TotalServicePrice,
        booking.DepositRequiredAmount,
        booking.DepositPaidAmount,
        booking.Currency,
        booking.ExpiresAtUtc,
        booking.CancellationReason,
        booking.RejectionReason,
        booking.Notes,
        booking.IdempotencyKey,
        booking.Version,
        booking.CreatedAtUtc,
        booking.UpdatedAtUtc);
}
