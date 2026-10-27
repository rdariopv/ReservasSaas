using FluentAssertions;
using Moq;
using ReservasSaaS.Application.DTOs.Bookings;
using ReservasSaaS.Application.UseCases.Bookings;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.Ports.Outgoing;
using Xunit;

namespace ReservasSaaS.UnitTests.Bookings;

public class BookingTenantIsolationTests
{
    private readonly Mock<IBookingRepository> _bookingRepoMock = new();
    private readonly Mock<IDateTimeProvider> _clockMock = new();

    private readonly Guid _companyA = Guid.NewGuid();
    private readonly Guid _companyB = Guid.NewGuid();
    private readonly Guid _bookingOfCompanyA = Guid.NewGuid();

    public BookingTenantIsolationTests()
    {
        _clockMock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

        // Booking belongs to Company A
        var bookingA = new Booking(
            id: _bookingOfCompanyA,
            companyId: _companyA,
            serviceId: Guid.NewGuid(),
            serviceName: "Servicio Privado Empresa A",
            professionalId: Guid.NewGuid(),
            professionalName: "Prof A",
            customerName: "Cliente A",
            customerEmail: "a@test.com",
            customerPhone: "+573001112233",
            scheduledStartTimeUtc: DateTime.UtcNow.AddHours(2),
            scheduledEndTimeUtc: DateTime.UtcNow.AddHours(3),
            totalServicePrice: 50000m,
            depositRequiredAmount: 0m);

        // When queried with Company A, repository returns booking
        _bookingRepoMock.Setup(r => r.GetByIdAsync(_companyA, _bookingOfCompanyA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookingA);

        // When queried with Company B, repository returns null (strict multi-tenant isolation)
        _bookingRepoMock.Setup(r => r.GetByIdAsync(_companyB, _bookingOfCompanyA, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);
    }

    [Fact]
    public async Task GetBooking_WhenCompanyMismatch_ThrowsEntityNotFoundException()
    {
        // Arrange
        var getUseCase = new GetBookingUseCase(_bookingRepoMock.Object);

        // Act: Company B tries to view booking belonging to Company A
        var act = async () => await getUseCase.ExecuteAsync(_companyB, _bookingOfCompanyA);

        // Assert: Must not expose or leak Company A's booking data
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"*{nameof(Booking)}*{_bookingOfCompanyA}*");
    }

    [Fact]
    public async Task CancelBooking_WhenCompanyMismatch_ThrowsEntityNotFoundException()
    {
        // Arrange
        var cancelUseCase = new CancelBookingUseCase(_bookingRepoMock.Object, _clockMock.Object);

        // Act: Company B tries to cancel Company A's booking
        var act = async () => await cancelUseCase.ExecuteAsync(
            _companyB, 
            _bookingOfCompanyA, 
            new CancelBookingCommand("Intento no autorizado de cancelacion cruzada"));

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>();
        _bookingRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
