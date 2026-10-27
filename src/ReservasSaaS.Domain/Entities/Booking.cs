using ReservasSaaS.Domain.Common;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;

namespace ReservasSaaS.Domain.Entities;

/// <summary>
/// Booking aggregate root managing appointment lifecycle, time reservations,
/// multi-professional allocation, revalidation invariants, and payment state decoupling.
/// </summary>
public class Booking : BaseEntity<Guid>, IAggregateRoot
{
    public Guid CompanyId { get; private set; }
    public Guid ServiceId { get; private set; }
    public string ServiceName { get; private set; }
    public Guid? ProfessionalId { get; private set; }
    public string? ProfessionalName { get; private set; }
    public string CustomerName { get; private set; }
    public string CustomerEmail { get; private set; }
    public string CustomerPhone { get; private set; }
    public DateTime ScheduledStartTimeUtc { get; private set; }
    public DateTime ScheduledEndTimeUtc { get; private set; }
    public BookingStatus Status { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public decimal TotalServicePrice { get; private set; }
    public decimal DepositRequiredAmount { get; private set; }
    public decimal DepositPaidAmount { get; private set; }
    public string Currency { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? Notes { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public int Version { get; private set; }

    // Required for deserialization/mapping
    private Booking()
    {
        ServiceName = string.Empty;
        CustomerName = string.Empty;
        CustomerEmail = string.Empty;
        CustomerPhone = string.Empty;
        Currency = "COP";
    }

    public Booking(
        Guid id,
        Guid companyId,
        Guid serviceId,
        string serviceName,
        Guid? professionalId,
        string? professionalName,
        string customerName,
        string customerEmail,
        string customerPhone,
        DateTime scheduledStartTimeUtc,
        DateTime scheduledEndTimeUtc,
        decimal totalServicePrice,
        decimal depositRequiredAmount,
        string currency = "COP",
        string? notes = null,
        string? idempotencyKey = null,
        TimeSpan? expirationWindow = null,
        DateTime? utcNow = null)
        : base(id)
    {
        var now = utcNow ?? DateTime.UtcNow;

        if (companyId == Guid.Empty)
            throw new ValidationException("CompanyId cannot be empty.");

        if (serviceId == Guid.Empty)
            throw new ValidationException("ServiceId cannot be empty.");

        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ValidationException("ServiceName cannot be empty.");

        if (string.IsNullOrWhiteSpace(customerName))
            throw new ValidationException("CustomerName cannot be empty.");

        if (string.IsNullOrWhiteSpace(customerPhone))
            throw new ValidationException("CustomerPhone cannot be empty.");

        if (scheduledEndTimeUtc <= scheduledStartTimeUtc)
            throw new ValidationException("Scheduled end time must be after scheduled start time.");

        if (totalServicePrice < 0)
            throw new ValidationException("Total service price cannot be negative.");

        if (depositRequiredAmount < 0 || depositRequiredAmount > totalServicePrice)
            throw new ValidationException("Deposit required amount must be between 0 and the total service price.");

        CompanyId = companyId;
        ServiceId = serviceId;
        ServiceName = serviceName.Trim();
        ProfessionalId = professionalId;
        ProfessionalName = professionalName?.Trim();
        CustomerName = customerName.Trim();
        CustomerEmail = customerEmail?.Trim() ?? string.Empty;
        CustomerPhone = customerPhone.Trim();
        ScheduledStartTimeUtc = scheduledStartTimeUtc;
        ScheduledEndTimeUtc = scheduledEndTimeUtc;
        TotalServicePrice = totalServicePrice;
        DepositRequiredAmount = depositRequiredAmount;
        DepositPaidAmount = 0m;
        Currency = currency;
        Notes = notes?.Trim();
        IdempotencyKey = idempotencyKey?.Trim();
        Version = 1;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;

        // Payment status initialization
        if (depositRequiredAmount > 0)
        {
            PaymentStatus = PaymentStatus.Pending;
        }
        else
        {
            PaymentStatus = PaymentStatus.NotRequired;
        }

        // Determine initial booking status
        if (!professionalId.HasValue)
        {
            Status = BookingStatus.PendingAssignment;
            ExpiresAtUtc = now.Add(expirationWindow ?? TimeSpan.FromHours(24));
        }
        else if (depositRequiredAmount > 0)
        {
            Status = BookingStatus.PendingPayment;
            ExpiresAtUtc = now.Add(expirationWindow ?? TimeSpan.FromMinutes(30));
        }
        else
        {
            Status = BookingStatus.Confirmed;
            ExpiresAtUtc = null;
        }
    }

    /// <summary>
    /// Factory method for repository reconstitutions from persistent storage.
    /// </summary>
    public static Booking Reconstitute(
        Guid id,
        Guid companyId,
        Guid serviceId,
        string serviceName,
        Guid? professionalId,
        string? professionalName,
        string customerName,
        string customerEmail,
        string customerPhone,
        DateTime scheduledStartTimeUtc,
        DateTime scheduledEndTimeUtc,
        BookingStatus status,
        PaymentStatus paymentStatus,
        decimal totalServicePrice,
        decimal depositRequiredAmount,
        decimal depositPaidAmount,
        string currency,
        DateTime? expiresAtUtc,
        string? cancellationReason,
        string? rejectionReason,
        string? notes,
        string? idempotencyKey,
        int version,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        var booking = new Booking
        {
            Id = id,
            CompanyId = companyId,
            ServiceId = serviceId,
            ServiceName = serviceName,
            ProfessionalId = professionalId,
            ProfessionalName = professionalName,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            CustomerPhone = customerPhone,
            ScheduledStartTimeUtc = scheduledStartTimeUtc,
            ScheduledEndTimeUtc = scheduledEndTimeUtc,
            Status = status,
            PaymentStatus = paymentStatus,
            TotalServicePrice = totalServicePrice,
            DepositRequiredAmount = depositRequiredAmount,
            DepositPaidAmount = depositPaidAmount,
            Currency = currency,
            ExpiresAtUtc = expiresAtUtc,
            CancellationReason = cancellationReason,
            RejectionReason = rejectionReason,
            Notes = notes,
            IdempotencyKey = idempotencyKey,
            Version = version,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc
        };

        return booking;
    }

    /// <summary>
    /// Assigns a designated professional to a pending booking.
    /// </summary>
    public void AssignProfessional(Guid professionalId, string professionalName, DateTime utcNow)
    {
        EnsureModifiable("assign a professional to");

        if (professionalId == Guid.Empty)
            throw new ValidationException("ProfessionalId must be valid.");

        if (string.IsNullOrWhiteSpace(professionalName))
            throw new ValidationException("ProfessionalName cannot be empty.");

        ProfessionalId = professionalId;
        ProfessionalName = professionalName.Trim();
        UpdatedAtUtc = utcNow;
        Version++;

        // If it was waiting for assignment:
        if (Status == BookingStatus.PendingAssignment)
        {
            if (DepositRequiredAmount > 0 && PaymentStatus != PaymentStatus.DepositPaid && PaymentStatus != PaymentStatus.PaidInFull)
            {
                Status = BookingStatus.PendingPayment;
            }
            else
            {
                Status = BookingStatus.Confirmed;
                ExpiresAtUtc = null;
            }
        }
    }

    /// <summary>
    /// Confirms a booking, validating that a professional is assigned and business rules are met.
    /// </summary>
    public void Confirm(DateTime utcNow)
    {
        EnsureModifiable("confirm");

        if (!ProfessionalId.HasValue)
            throw new ValidationException("Cannot confirm booking without an assigned professional.");

        if (DepositRequiredAmount > 0 &&
            PaymentStatus != PaymentStatus.DepositPaid &&
            PaymentStatus != PaymentStatus.PaidInFull &&
            PaymentStatus != PaymentStatus.SubmittedForReview)
        {
            throw new ValidationException("Cannot confirm booking: required deposit has not been paid or submitted for review.");
        }

        Status = BookingStatus.Confirmed;
        ExpiresAtUtc = null;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    /// <summary>
    /// Reschedules the appointment to a new date and time window.
    /// </summary>
    public void Reschedule(
        DateTime newStartTimeUtc, 
        DateTime newEndTimeUtc, 
        Guid? newProfessionalId, 
        string? newProfessionalName, 
        DateTime utcNow)
    {
        if (Status is BookingStatus.Completed or BookingStatus.Cancelled or BookingStatus.Expired or BookingStatus.Rejected or BookingStatus.NoShow)
        {
            throw new ValidationException($"Cannot reschedule a booking with status '{Status}'.");
        }

        if (newEndTimeUtc <= newStartTimeUtc)
            throw new ValidationException("New end time must be after new start time.");

        ScheduledStartTimeUtc = newStartTimeUtc;
        ScheduledEndTimeUtc = newEndTimeUtc;

        if (newProfessionalId.HasValue)
        {
            ProfessionalId = newProfessionalId.Value;
            ProfessionalName = newProfessionalName?.Trim() ?? ProfessionalName;
        }

        UpdatedAtUtc = utcNow;
        Version++;
    }

    /// <summary>
    /// Cancels the booking with an explanatory reason.
    /// </summary>
    public void Cancel(string reason, DateTime utcNow)
    {
        if (Status is BookingStatus.Completed)
            throw new ValidationException("Cannot cancel an already completed booking.");

        if (Status is BookingStatus.Cancelled)
            return; // Idempotent

        Status = BookingStatus.Cancelled;
        CancellationReason = reason?.Trim() ?? "Cancelada por el usuario o administración";
        ExpiresAtUtc = null;
        UpdatedAtUtc = utcNow;
        Version++;

        if (DepositPaidAmount > 0)
        {
            PaymentStatus = PaymentStatus.Refunded;
        }
    }

    /// <summary>
    /// Rejects the booking (e.g. invalid receipt, business unavailability).
    /// </summary>
    public void Reject(string reason, DateTime utcNow)
    {
        if (Status is BookingStatus.Completed or BookingStatus.Cancelled)
            throw new ValidationException($"Cannot reject a booking with status '{Status}'.");

        Status = BookingStatus.Rejected;
        RejectionReason = reason?.Trim() ?? "Reserva rechazada por el negocio";
        PaymentStatus = PaymentStatus.Failed;
        ExpiresAtUtc = null;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    /// <summary>
    /// Transitions booking to Expired when timeout expires.
    /// </summary>
    public void Expire(DateTime utcNow)
    {
        if (Status is not (BookingStatus.PendingPayment or BookingStatus.PendingAssignment or BookingStatus.PaymentReview))
            throw new ValidationException($"Cannot expire a booking with status '{Status}'. Only pending bookings can expire.");

        Status = BookingStatus.Expired;
        CancellationReason = "Tiempo límite de confirmación/pago expirado";
        ExpiresAtUtc = null;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    /// <summary>
    /// Records client check-in at the facility.
    /// </summary>
    public void CheckIn(DateTime utcNow)
    {
        if (Status is not BookingStatus.Confirmed)
            throw new ValidationException($"Only Confirmed bookings can be checked in. Current status: '{Status}'.");

        Status = BookingStatus.CheckedIn;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    /// <summary>
    /// Starts the appointment service.
    /// </summary>
    public void StartService(DateTime utcNow)
    {
        if (Status is not (BookingStatus.Confirmed or BookingStatus.CheckedIn))
            throw new ValidationException($"Cannot start service from status '{Status}'. Booking must be Confirmed or CheckedIn.");

        Status = BookingStatus.InProgress;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    /// <summary>
    /// Marks the booking as successfully completed.
    /// </summary>
    public void CompleteService(DateTime utcNow)
    {
        if (Status is not (BookingStatus.InProgress or BookingStatus.CheckedIn or BookingStatus.Confirmed))
            throw new ValidationException($"Cannot complete service from status '{Status}'.");

        Status = BookingStatus.Completed;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    /// <summary>
    /// Marks the booking as a customer no-show.
    /// </summary>
    public void MarkNoShow(DateTime utcNow)
    {
        if (Status is not (BookingStatus.Confirmed or BookingStatus.CheckedIn))
            throw new ValidationException($"Cannot mark NoShow from status '{Status}'.");

        Status = BookingStatus.NoShow;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    /// <summary>
    /// Updates independent payment state and paid amounts, preserving transaction references.
    /// </summary>
    public void UpdatePayment(PaymentStatus status, decimal paidAmount, DateTime utcNow, string? transactionReference = null)
    {
        PaymentStatus = status;
        DepositPaidAmount = Math.Max(DepositPaidAmount, paidAmount);
        if (!string.IsNullOrWhiteSpace(transactionReference))
        {
            var refText = $"[Pago Ref: {transactionReference.Trim()}]";
            Notes = string.IsNullOrWhiteSpace(Notes) ? refText : $"{Notes} | {refText}";
        }
        UpdatedAtUtc = utcNow;
        Version++;

        // If client submitted payment for review, update booking status
        if (status == PaymentStatus.SubmittedForReview && Status == BookingStatus.PendingPayment)
        {
            Status = BookingStatus.PaymentReview;
        }
        else if (status == PaymentStatus.Failed)
        {
            if (string.IsNullOrWhiteSpace(RejectionReason))
            {
                RejectionReason = "Comprobante de pago rechazado";
            }
        }
        else if ((status == PaymentStatus.DepositPaid || status == PaymentStatus.PaidInFull) && 
                 (Status == BookingStatus.PendingPayment || Status == BookingStatus.PaymentReview))
        {
            if (ProfessionalId.HasValue)
            {
                Status = BookingStatus.Confirmed;
                ExpiresAtUtc = null;
            }
            else
            {
                Status = BookingStatus.PendingAssignment;
            }
        }
    }

    private void EnsureModifiable(string action)
    {
        if (Status is BookingStatus.Cancelled or BookingStatus.Completed or BookingStatus.Expired or BookingStatus.Rejected or BookingStatus.NoShow)
        {
            throw new ValidationException($"Cannot {action} booking '{Id}' because it is in terminal status '{Status}'.");
        }
    }
}
