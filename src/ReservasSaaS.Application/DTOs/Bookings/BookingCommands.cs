using ReservasSaaS.Domain.Enums;

namespace ReservasSaaS.Application.DTOs.Bookings;

public record CreateBookingCommand(
    Guid ServiceId,
    DateTime StartTimeUtc,
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail = null,
    Guid? ProfessionalId = null,
    string? Notes = null,
    string? IdempotencyKey = null);

public record RescheduleBookingCommand(
    DateTime NewStartTimeUtc,
    Guid? NewProfessionalId = null,
    string? Reason = null);

public record AssignProfessionalCommand(
    Guid ProfessionalId);

public record CancelBookingCommand(
    string Reason);

public record RejectBookingCommand(
    string Reason);

public record ConfirmBookingCommand();

public record UpdateBookingPaymentCommand(
    PaymentStatus PaymentStatus,
    decimal PaidAmount = 0,
    string? TransactionReference = null);

public record BookingFilterDto(
    BookingStatus? Status = null,
    Guid? ProfessionalId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    PaymentStatus? PaymentStatus = null,
    int Page = 1,
    int PageSize = 20);
