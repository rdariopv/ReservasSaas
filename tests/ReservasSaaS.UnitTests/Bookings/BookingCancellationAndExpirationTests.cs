using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReservasSaaS.Application.DTOs.Bookings;
using ReservasSaaS.Application.UseCases.Bookings;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Ports.Outgoing;
using Xunit;

namespace ReservasSaaS.UnitTests.Bookings;

public class BookingCancellationAndExpirationTests
{
    private readonly Mock<IBookingRepository> _bookingRepoMock = new();
    private readonly Mock<IDateTimeProvider> _clockMock = new();

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _bookingId = Guid.NewGuid();
    private readonly DateTime _nowUtc = new(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

    public BookingCancellationAndExpirationTests()
    {
        _clockMock.Setup(c => c.UtcNow).Returns(_nowUtc);
    }

    [Fact]
    public async Task CancelBooking_WhenActive_TransitionsToCancelledWithReason()
    {
        // Arrange
        var booking = new Booking(
            id: _bookingId,
            companyId: _companyId,
            serviceId: Guid.NewGuid(),
            serviceName: "Terapia",
            professionalId: Guid.NewGuid(),
            professionalName: "Dr. Morales",
            customerName: "Diana Gomez",
            customerEmail: "diana@test.com",
            customerPhone: "+573001234567",
            scheduledStartTimeUtc: _nowUtc.AddDays(1),
            scheduledEndTimeUtc: _nowUtc.AddDays(1).AddHours(1),
            totalServicePrice: 100000m,
            depositRequiredAmount: 0m);

        _bookingRepoMock.Setup(r => r.GetByIdAsync(_companyId, _bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var cancelUseCase = new CancelBookingUseCase(
            _bookingRepoMock.Object,
            _clockMock.Object);

        var command = new CancelBookingCommand("Emergencia familiar del cliente");

        // Act
        var result = await cancelUseCase.ExecuteAsync(_companyId, _bookingId, command);

        // Assert
        result.Status.Should().Be(BookingStatus.Cancelled.ToString());
        result.CancellationReason.Should().Be("Emergencia familiar del cliente");
        _bookingRepoMock.Verify(r => r.UpdateAsync(It.Is<Booking>(b => b.Status == BookingStatus.Cancelled), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExpireStaleBookings_FindsPastDeadlineBookings_TransitionsToExpired()
    {
        // Arrange: 2 stale bookings past their expiration deadline
        var stale1 = new Booking(
            id: Guid.NewGuid(),
            companyId: _companyId,
            serviceId: Guid.NewGuid(),
            serviceName: "Spa",
            professionalId: Guid.NewGuid(),
            professionalName: "Ana",
            customerName: "Cliente 1",
            customerEmail: "c1@test.com",
            customerPhone: "+573001111111",
            scheduledStartTimeUtc: _nowUtc.AddHours(2),
            scheduledEndTimeUtc: _nowUtc.AddHours(3),
            totalServicePrice: 90000m,
            depositRequiredAmount: 30000m,
            utcNow: _nowUtc.AddHours(-1)); // Created an hour ago with 30m window => Expired!

        var stale2 = new Booking(
            id: Guid.NewGuid(),
            companyId: _companyId,
            serviceId: Guid.NewGuid(),
            serviceName: "Corte",
            professionalId: null,
            professionalName: null,
            customerName: "Cliente 2",
            customerEmail: "c2@test.com",
            customerPhone: "+573002222222",
            scheduledStartTimeUtc: _nowUtc.AddHours(4),
            scheduledEndTimeUtc: _nowUtc.AddHours(4.5),
            totalServicePrice: 35000m,
            depositRequiredAmount: 0m,
            utcNow: _nowUtc.AddDays(-2)); // Created 2 days ago => Expired!

        _bookingRepoMock.Setup(r => r.GetStalePendingBookingsAsync(_companyId, _nowUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { stale1, stale2 });

        var expireUseCase = new ExpireStaleBookingsUseCase(
            _bookingRepoMock.Object,
            _clockMock.Object,
            NullLogger<ExpireStaleBookingsUseCase>.Instance);

        // Act
        var expiredCount = await expireUseCase.ExecuteAsync(_companyId);

        // Assert
        expiredCount.Should().Be(2);
        stale1.Status.Should().Be(BookingStatus.Expired);
        stale2.Status.Should().Be(BookingStatus.Expired);

        _bookingRepoMock.Verify(r => r.UpdateAsync(It.Is<Booking>(b => b.Status == BookingStatus.Expired), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
