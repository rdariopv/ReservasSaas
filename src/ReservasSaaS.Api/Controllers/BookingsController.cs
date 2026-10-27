using Microsoft.AspNetCore.Mvc;
using ReservasSaaS.Application.Common;
using ReservasSaaS.Application.DTOs.Bookings;
using ReservasSaaS.Application.Ports.Incoming;

namespace ReservasSaaS.Api.Controllers;

[ApiController]
[Route("api/v1/companies/{companyId:guid}/[controller]")]
[Produces("application/json")]
public class BookingsController : ControllerBase
{
    private readonly ICreateBookingUseCase _createBookingUseCase;
    private readonly IGetBookingUseCase _getBookingUseCase;
    private readonly ISearchBookingsUseCase _searchBookingsUseCase;
    private readonly IRescheduleBookingUseCase _rescheduleBookingUseCase;
    private readonly IAssignProfessionalUseCase _assignProfessionalUseCase;
    private readonly IConfirmBookingUseCase _confirmBookingUseCase;
    private readonly ICancelBookingUseCase _cancelBookingUseCase;
    private readonly IFinalizeBookingUseCase _finalizeBookingUseCase;
    private readonly IExpireStaleBookingsUseCase _expireStaleBookingsUseCase;
    private readonly IUpdateBookingPaymentUseCase _updateBookingPaymentUseCase;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(
        ICreateBookingUseCase createBookingUseCase,
        IGetBookingUseCase getBookingUseCase,
        ISearchBookingsUseCase searchBookingsUseCase,
        IRescheduleBookingUseCase rescheduleBookingUseCase,
        IAssignProfessionalUseCase assignProfessionalUseCase,
        IConfirmBookingUseCase confirmBookingUseCase,
        ICancelBookingUseCase cancelBookingUseCase,
        IFinalizeBookingUseCase finalizeBookingUseCase,
        IExpireStaleBookingsUseCase expireStaleBookingsUseCase,
        IUpdateBookingPaymentUseCase updateBookingPaymentUseCase,
        ILogger<BookingsController> logger)
    {
        _createBookingUseCase = createBookingUseCase;
        _getBookingUseCase = getBookingUseCase;
        _searchBookingsUseCase = searchBookingsUseCase;
        _rescheduleBookingUseCase = rescheduleBookingUseCase;
        _assignProfessionalUseCase = assignProfessionalUseCase;
        _confirmBookingUseCase = confirmBookingUseCase;
        _cancelBookingUseCase = cancelBookingUseCase;
        _finalizeBookingUseCase = finalizeBookingUseCase;
        _expireStaleBookingsUseCase = expireStaleBookingsUseCase;
        _updateBookingPaymentUseCase = updateBookingPaymentUseCase;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new booking with automatic idempotency check and capacity revalidation.
    /// Supports the Idempotency-Key HTTP header for duplicate prevention.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBooking(
        Guid companyId,
        [FromBody] CreateBookingCommand command,
        [FromHeader(Name = "Idempotency-Key")] string? headerIdempotencyKey,
        CancellationToken cancellationToken)
    {
        // Header takes precedence if provided
        var effectiveKey = !string.IsNullOrWhiteSpace(headerIdempotencyKey) 
            ? headerIdempotencyKey 
            : command.IdempotencyKey;

        var effectiveCommand = command with { IdempotencyKey = effectiveKey };

        var booking = await _createBookingUseCase.ExecuteAsync(companyId, effectiveCommand, cancellationToken);
        return CreatedAtAction(nameof(GetBookingById), new { companyId, bookingId = booking.Id }, ApiResponse<BookingDto>.Ok(booking));
    }

    /// <summary>
    /// Searches and filters bookings by status, professional, and date window.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BookingDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchBookings(
        Guid companyId,
        [FromQuery] BookingFilterDto filter,
        CancellationToken cancellationToken)
    {
        var (items, count) = await _searchBookingsUseCase.ExecuteAsync(companyId, filter, cancellationToken);
        var response = ApiResponse<IReadOnlyList<BookingDto>>.Ok(items, $"Total {count} bookings found.");
        return Ok(response);
    }

    /// <summary>
    /// Retrieves full details of a specific booking.
    /// </summary>
    [HttpGet("{bookingId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingById(
        Guid companyId,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var booking = await _getBookingUseCase.ExecuteAsync(companyId, bookingId, cancellationToken);
        return Ok(ApiResponse<BookingDto>.Ok(booking));
    }

    /// <summary>
    /// Reschedules a booking to a new time window, revalidating professional capacity.
    /// </summary>
    [HttpPost("{bookingId:guid}/reschedule")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RescheduleBooking(
        Guid companyId,
        Guid bookingId,
        [FromBody] RescheduleBookingCommand command,
        CancellationToken cancellationToken)
    {
        var booking = await _rescheduleBookingUseCase.ExecuteAsync(companyId, bookingId, command, cancellationToken);
        return Ok(ApiResponse<BookingDto>.Ok(booking, "Booking rescheduled successfully."));
    }

    /// <summary>
    /// Assigns a designated professional to a pending booking.
    /// </summary>
    [HttpPost("{bookingId:guid}/assign-professional")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignProfessional(
        Guid companyId,
        Guid bookingId,
        [FromBody] AssignProfessionalCommand command,
        CancellationToken cancellationToken)
    {
        var booking = await _assignProfessionalUseCase.ExecuteAsync(companyId, bookingId, command, cancellationToken);
        return Ok(ApiResponse<BookingDto>.Ok(booking, "Professional assigned successfully."));
    }

    /// <summary>
    /// Confirms a booking, ensuring professional assignment and payment policy requirements.
    /// </summary>
    [HttpPost("{bookingId:guid}/confirm")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmBooking(
        Guid companyId,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var booking = await _confirmBookingUseCase.ExecuteAsync(companyId, bookingId, cancellationToken);
        return Ok(ApiResponse<BookingDto>.Ok(booking, "Booking confirmed successfully."));
    }

    /// <summary>
    /// Cancels an active booking with an explanatory reason.
    /// </summary>
    [HttpPost("{bookingId:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelBooking(
        Guid companyId,
        Guid bookingId,
        [FromBody] CancelBookingCommand command,
        CancellationToken cancellationToken)
    {
        var booking = await _cancelBookingUseCase.ExecuteAsync(companyId, bookingId, command, cancellationToken);
        return Ok(ApiResponse<BookingDto>.Ok(booking, "Booking cancelled successfully."));
    }

    /// <summary>
    /// Rejects a booking (e.g. invalid payment proof or unavailable capacity).
    /// </summary>
    [HttpPost("{bookingId:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectBooking(
        Guid companyId,
        Guid bookingId,
        [FromBody] RejectBookingCommand command,
        CancellationToken cancellationToken)
    {
        var booking = await _finalizeBookingUseCase.RejectAsync(companyId, bookingId, command, cancellationToken);
        return Ok(ApiResponse<BookingDto>.Ok(booking, "Booking rejected."));
    }

    /// <summary>
    /// Checks in the client upon arrival at the premises.
    /// </summary>
    [HttpPost("{bookingId:guid}/check-in")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckIn(Guid companyId, Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await _finalizeBookingUseCase.CheckInAsync(companyId, bookingId, cancellationToken);
        return Ok(ApiResponse<BookingDto>.Ok(booking, "Client checked in successfully."));
    }

    /// <summary>
    /// Marks the booked service as InProgress.
    /// </summary>
    [HttpPost("{bookingId:guid}/start")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> StartService(Guid companyId, Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await _finalizeBookingUseCase.StartServiceAsync(companyId, bookingId, cancellationToken);
        return Ok(ApiResponse<BookingDto>.Ok(booking, "Service is now in progress."));
    }

    /// <summary>
    /// Marks the service as Completed.
    /// </summary>
    [HttpPost("{bookingId:guid}/complete")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteService(Guid companyId, Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await _finalizeBookingUseCase.CompleteServiceAsync(companyId, bookingId, cancellationToken);
        return Ok(ApiResponse<BookingDto>.Ok(booking, "Service completed successfully."));
    }

    /// <summary>
    /// Marks the client as a NoShow.
    /// </summary>
    [HttpPost("{bookingId:guid}/no-show")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkNoShow(Guid companyId, Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await _finalizeBookingUseCase.MarkNoShowAsync(companyId, bookingId, cancellationToken);
        return Ok(ApiResponse<BookingDto>.Ok(booking, "Booking marked as NoShow."));
    }

    /// <summary>
    /// Updates the independent payment status for the booking.
    /// </summary>
    [HttpPost("{bookingId:guid}/payment-status")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePaymentStatus(
        Guid companyId,
        Guid bookingId,
        [FromBody] UpdateBookingPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var booking = await _updateBookingPaymentUseCase.ExecuteAsync(companyId, bookingId, command, cancellationToken);
        return Ok(ApiResponse<BookingDto>.Ok(booking, "Payment status updated successfully."));
    }

    /// <summary>
    /// Triggers expiration check for stale pending bookings past their expiration deadline.
    /// </summary>
    [HttpPost("expire-stale")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExpireStale(Guid companyId, CancellationToken cancellationToken)
    {
        var expiredCount = await _expireStaleBookingsUseCase.ExecuteAsync(companyId, cancellationToken);
        return Ok(ApiResponse<int>.Ok(expiredCount, $"Expired {expiredCount} stale bookings."));
    }
}
