using ReservasSaaS.Application.DTOs.Bookings;

namespace ReservasSaaS.Application.Ports.Incoming;

public interface ICreateBookingUseCase
{
    Task<BookingDto> ExecuteAsync(Guid companyId, CreateBookingCommand command, CancellationToken cancellationToken = default);
}

public interface IGetBookingUseCase
{
    Task<BookingDto> ExecuteAsync(Guid companyId, Guid bookingId, CancellationToken cancellationToken = default);
}

public interface ISearchBookingsUseCase
{
    Task<(IReadOnlyList<BookingDto> Items, int TotalCount)> ExecuteAsync(
        Guid companyId, 
        BookingFilterDto filter, 
        CancellationToken cancellationToken = default);
}

public interface IRescheduleBookingUseCase
{
    Task<BookingDto> ExecuteAsync(
        Guid companyId, 
        Guid bookingId, 
        RescheduleBookingCommand command, 
        CancellationToken cancellationToken = default);
}

public interface IAssignProfessionalUseCase
{
    Task<BookingDto> ExecuteAsync(
        Guid companyId, 
        Guid bookingId, 
        AssignProfessionalCommand command, 
        CancellationToken cancellationToken = default);
}

public interface IConfirmBookingUseCase
{
    Task<BookingDto> ExecuteAsync(
        Guid companyId, 
        Guid bookingId, 
        CancellationToken cancellationToken = default);
}

public interface ICancelBookingUseCase
{
    Task<BookingDto> ExecuteAsync(
        Guid companyId, 
        Guid bookingId, 
        CancelBookingCommand command, 
        CancellationToken cancellationToken = default);
}

public interface IFinalizeBookingUseCase
{
    Task<BookingDto> CheckInAsync(Guid companyId, Guid bookingId, CancellationToken cancellationToken = default);
    Task<BookingDto> StartServiceAsync(Guid companyId, Guid bookingId, CancellationToken cancellationToken = default);
    Task<BookingDto> CompleteServiceAsync(Guid companyId, Guid bookingId, CancellationToken cancellationToken = default);
    Task<BookingDto> MarkNoShowAsync(Guid companyId, Guid bookingId, CancellationToken cancellationToken = default);
    Task<BookingDto> RejectAsync(Guid companyId, Guid bookingId, RejectBookingCommand command, CancellationToken cancellationToken = default);
}

public interface IExpireStaleBookingsUseCase
{
    Task<int> ExecuteAsync(Guid companyId, CancellationToken cancellationToken = default);
}

public interface IUpdateBookingPaymentUseCase
{
    Task<BookingDto> ExecuteAsync(
        Guid companyId, 
        Guid bookingId, 
        UpdateBookingPaymentCommand command, 
        CancellationToken cancellationToken = default);
}
