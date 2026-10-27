using Microsoft.Extensions.Logging;
using ReservasSaaS.Application.DTOs.Bookings;
using ReservasSaaS.Application.Ports.Incoming;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.Ports.Outgoing;

namespace ReservasSaaS.Application.UseCases.Bookings;

public class GetBookingUseCase : IGetBookingUseCase
{
    private readonly IBookingRepository _bookingRepository;

    public GetBookingUseCase(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<BookingDto> ExecuteAsync(Guid companyId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(companyId, bookingId, cancellationToken);
        if (booking == null)
            throw new EntityNotFoundException(nameof(Booking), bookingId);

        return BookingDto.FromDomain(booking);
    }
}

public class SearchBookingsUseCase : ISearchBookingsUseCase
{
    private readonly IBookingRepository _bookingRepository;

    public SearchBookingsUseCase(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<(IReadOnlyList<BookingDto> Items, int TotalCount)> ExecuteAsync(
        Guid companyId, 
        BookingFilterDto filter, 
        CancellationToken cancellationToken = default)
    {
        var (items, count) = await _bookingRepository.SearchAsync(
            companyId,
            filter.Status,
            filter.ProfessionalId,
            filter.FromUtc,
            filter.ToUtc,
            filter.Page,
            filter.PageSize,
            filter.PaymentStatus,
            cancellationToken);

        var dtos = items.Select(BookingDto.FromDomain).ToList();
        return (dtos, count);
    }
}

public class RescheduleBookingUseCase : IRescheduleBookingUseCase
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IProfessionalRepository _professionalRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<RescheduleBookingUseCase> _logger;

    public RescheduleBookingUseCase(
        IBookingRepository bookingRepository,
        IServiceRepository serviceRepository,
        IProfessionalRepository professionalRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<RescheduleBookingUseCase> logger)
    {
        _bookingRepository = bookingRepository;
        _serviceRepository = serviceRepository;
        _professionalRepository = professionalRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<BookingDto> ExecuteAsync(
        Guid companyId, 
        Guid bookingId, 
        RescheduleBookingCommand command, 
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var booking = await _bookingRepository.GetByIdAsync(companyId, bookingId, cancellationToken);
        if (booking == null)
            throw new EntityNotFoundException(nameof(Booking), bookingId);

        var service = await _serviceRepository.GetByIdAsync(companyId.ToString(), booking.ServiceId.ToString(), cancellationToken);
        if (service == null)
            throw new EntityNotFoundException(nameof(Service), booking.ServiceId);

        var newStartUtc = command.NewStartTimeUtc;
        var newEndUtc = newStartUtc.AddMinutes(service.DurationMinutes);

        var targetProfId = command.NewProfessionalId ?? booking.ProfessionalId;
        string? targetProfName = null;

        if (targetProfId.HasValue)
        {
            var prof = await _professionalRepository.GetByUserIdAsync (companyId, targetProfId.Value, cancellationToken);
            if (prof == null)
                throw new EntityNotFoundException(nameof(Professional), targetProfId.Value);

            if (!prof.IsActive || !prof.CanProvideService(service.Id))
                throw new ValidationException($"Professional '{prof.Name}' cannot perform service '{service.Name}'.");

            // Check overlaps for target professional
            var overlaps = await _bookingRepository.GetOverlappingBookingsAsync(
                companyId, prof.Id, newStartUtc, newEndUtc, booking.Id, cancellationToken);

            if (overlaps.Count > 0)
            {
                throw new ConcurrencyException(
                    $"El profesional '{prof.Name}' ya tiene una reserva en el nuevo horario solicitado ({newStartUtc:HH:mm} - {newEndUtc:HH:mm} UTC).");
            }

            targetProfName = prof.Name;
        }

        booking.Reschedule(newStartUtc, newEndUtc, targetProfId, targetProfName, nowUtc);
        await _bookingRepository.UpdateAsync(booking, cancellationToken);

        _logger.LogInformation("Booking '{BookingId}' rescheduled to {NewStartUtc}.", booking.Id, newStartUtc);
        return BookingDto.FromDomain(booking);
    }
}

public class AssignProfessionalUseCase : IAssignProfessionalUseCase
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IProfessionalRepository _professionalRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<AssignProfessionalUseCase> _logger;

    public AssignProfessionalUseCase(
        IBookingRepository bookingRepository,
        IProfessionalRepository professionalRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<AssignProfessionalUseCase> logger)
    {
        _bookingRepository = bookingRepository;
        _professionalRepository = professionalRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<BookingDto> ExecuteAsync(
        Guid companyId, 
        Guid bookingId, 
        AssignProfessionalCommand command, 
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var booking = await _bookingRepository.GetByIdAsync(companyId, bookingId, cancellationToken);
        if (booking == null)
            throw new EntityNotFoundException(nameof(Booking), bookingId);

        var professional = await _professionalRepository.GetByUserIdAsync(companyId, command.ProfessionalId, cancellationToken);
        if (professional == null)
            throw new EntityNotFoundException(nameof(Professional), command.ProfessionalId);

        if (!professional.IsActive)
            throw new ValidationException($"Professional '{professional.Name}' is not active.");

        if (!professional.CanProvideService(booking.ServiceId))
            throw new ValidationException($"Professional '{professional.Name}' is not enabled for service '{booking.ServiceName}'.");

        // Validate schedule conflicts
        var overlaps = await _bookingRepository.GetOverlappingBookingsAsync(
            companyId, 
            professional.Id, 
            booking.ScheduledStartTimeUtc, 
            booking.ScheduledEndTimeUtc, 
            booking.Id, 
            cancellationToken);

        if (overlaps.Count > 0)
        {
            throw new ConcurrencyException(
                $"El profesional '{professional.Name}' ya está ocupado en el horario de la reserva.");
        }

        booking.AssignProfessional(professional.Id, professional.Name, nowUtc);
        await _bookingRepository.UpdateAsync(booking, cancellationToken);

        _logger.LogInformation("Professional '{ProfName}' assigned to Booking '{BookingId}'.", professional.Name, booking.Id);
        return BookingDto.FromDomain(booking);
    }
}

public class ConfirmBookingUseCase : IConfirmBookingUseCase
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ConfirmBookingUseCase(
        IBookingRepository bookingRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _bookingRepository = bookingRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<BookingDto> ExecuteAsync(
        Guid companyId, 
        Guid bookingId, 
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var booking = await _bookingRepository.GetByIdAsync(companyId, bookingId, cancellationToken);
        if (booking == null)
            throw new EntityNotFoundException(nameof(Booking), bookingId);

        booking.Confirm(nowUtc);
        await _bookingRepository.UpdateAsync(booking, cancellationToken);

        return BookingDto.FromDomain(booking);
    }
}

public class CancelBookingUseCase : ICancelBookingUseCase
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CancelBookingUseCase(
        IBookingRepository bookingRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _bookingRepository = bookingRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<BookingDto> ExecuteAsync(
        Guid companyId, 
        Guid bookingId, 
        CancelBookingCommand command, 
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var booking = await _bookingRepository.GetByIdAsync(companyId, bookingId, cancellationToken);
        if (booking == null)
            throw new EntityNotFoundException(nameof(Booking), bookingId);

        booking.Cancel(command.Reason, nowUtc);
        await _bookingRepository.UpdateAsync(booking, cancellationToken);

        return BookingDto.FromDomain(booking);
    }
}

public class FinalizeBookingUseCase : IFinalizeBookingUseCase
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public FinalizeBookingUseCase(
        IBookingRepository bookingRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _bookingRepository = bookingRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<BookingDto> CheckInAsync(Guid companyId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var booking = await _bookingRepository.GetByIdAsync(companyId, bookingId, cancellationToken);
        if (booking == null)
            throw new EntityNotFoundException(nameof(Booking), bookingId);

        booking.CheckIn(nowUtc);
        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        return BookingDto.FromDomain(booking);
    }

    public async Task<BookingDto> StartServiceAsync(Guid companyId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var booking = await _bookingRepository.GetByIdAsync(companyId, bookingId, cancellationToken);
        if (booking == null)
            throw new EntityNotFoundException(nameof(Booking), bookingId);

        booking.StartService(nowUtc);
        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        return BookingDto.FromDomain(booking);
    }

    public async Task<BookingDto> CompleteServiceAsync(Guid companyId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var booking = await _bookingRepository.GetByIdAsync(companyId, bookingId, cancellationToken);
        if (booking == null)
            throw new EntityNotFoundException(nameof(Booking), bookingId);

        booking.CompleteService(nowUtc);
        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        return BookingDto.FromDomain(booking);
    }

    public async Task<BookingDto> MarkNoShowAsync(Guid companyId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var booking = await _bookingRepository.GetByIdAsync(companyId, bookingId, cancellationToken);
        if (booking == null)
            throw new EntityNotFoundException(nameof(Booking), bookingId);

        booking.MarkNoShow(nowUtc);
        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        return BookingDto.FromDomain(booking);
    }

    public async Task<BookingDto> RejectAsync(Guid companyId, Guid bookingId, RejectBookingCommand command, CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var booking = await _bookingRepository.GetByIdAsync(companyId, bookingId, cancellationToken);
        if (booking == null)
            throw new EntityNotFoundException(nameof(Booking), bookingId);

        booking.Reject(command.Reason, nowUtc);
        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        return BookingDto.FromDomain(booking);
    }
}

public class ExpireStaleBookingsUseCase : IExpireStaleBookingsUseCase
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ExpireStaleBookingsUseCase> _logger;

    public ExpireStaleBookingsUseCase(
        IBookingRepository bookingRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<ExpireStaleBookingsUseCase> logger)
    {
        _bookingRepository = bookingRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var staleBookings = await _bookingRepository.GetStalePendingBookingsAsync(companyId, nowUtc, cancellationToken);

        int count = 0;
        foreach (var booking in staleBookings)
        {
            try
            {
                booking.Expire(nowUtc);
                await _bookingRepository.UpdateAsync(booking, cancellationToken);
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to expire stale booking '{BookingId}'.", booking.Id);
            }
        }

        _logger.LogInformation("Expired {Count} stale bookings for Company '{CompanyId}'.", count, companyId);
        return count;
    }
}

public class UpdateBookingPaymentUseCase : IUpdateBookingPaymentUseCase
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateBookingPaymentUseCase(
        IBookingRepository bookingRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _bookingRepository = bookingRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<BookingDto> ExecuteAsync(
        Guid companyId, 
        Guid bookingId, 
        UpdateBookingPaymentCommand command, 
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var booking = await _bookingRepository.GetByIdAsync(companyId, bookingId, cancellationToken);
        if (booking == null)
            throw new EntityNotFoundException(nameof(Booking), bookingId);

        booking.UpdatePayment(command.PaymentStatus, command.PaidAmount, nowUtc, command.TransactionReference);
        await _bookingRepository.UpdateAsync(booking, cancellationToken);

        return BookingDto.FromDomain(booking);
    }
}
