namespace ReservasSaaS.Domain.Enums;

/// <summary>
/// Lifecycle status of an appointment/booking.
/// </summary>
public enum BookingStatus
{
    /// <summary>
    /// Booking created under BusinessAssignsProfessional mode awaiting professional assignment.
    /// </summary>
    PendingAssignment = 1,

    /// <summary>
    /// Booking created where a deposit is required and awaiting payment from the client.
    /// </summary>
    PendingPayment = 2,

    /// <summary>
    /// Client submitted payment proof; awaiting manual or automated validation.
    /// </summary>
    PaymentReview = 3,

    /// <summary>
    /// Booking is confirmed: professional is assigned, schedule is reserved, and payment policy satisfied.
    /// </summary>
    Confirmed = 4,

    /// <summary>
    /// Client arrived at the venue and checked in.
    /// </summary>
    CheckedIn = 5,

    /// <summary>
    /// Service is actively in progress.
    /// </summary>
    InProgress = 6,

    /// <summary>
    /// Service was successfully completed.
    /// </summary>
    Completed = 7,

    /// <summary>
    /// Booking was cancelled by client or business before execution.
    /// </summary>
    Cancelled = 8,

    /// <summary>
    /// Booking expired because required deposit or assignment was not completed within the allowed time window.
    /// </summary>
    Expired = 9,

    /// <summary>
    /// Booking was rejected by the business or payment review failed.
    /// </summary>
    Rejected = 10,

    /// <summary>
    /// Client failed to show up at the scheduled appointment time.
    /// </summary>
    NoShow = 11
}
