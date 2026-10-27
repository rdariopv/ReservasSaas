namespace ReservasSaaS.Domain.Enums;

/// <summary>
/// Dedicated payment status independent of booking lifecycle status.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Service does not require deposit or advance payment.
    /// </summary>
    NotRequired = 1,

    /// <summary>
    /// Advance payment/deposit is required and pending client execution.
    /// </summary>
    Pending = 2,

    /// <summary>
    /// Client submitted payment voucher/proof, awaiting verification.
    /// </summary>
    SubmittedForReview = 3,

    /// <summary>
    /// Required deposit was successfully paid.
    /// </summary>
    DepositPaid = 4,

    /// <summary>
    /// Total service price was paid in full.
    /// </summary>
    PaidInFull = 5,

    /// <summary>
    /// Payment was refunded due to cancellation or policy.
    /// </summary>
    Refunded = 6,

    /// <summary>
    /// Payment failed or proof was rejected.
    /// </summary>
    Failed = 7
}
