using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReservasSaaS.Application.DTOs.Bookings;
using ReservasSaaS.Application.UseCases.Bookings;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Domain.ValueObjects;
using System.Xml.Linq;
using Xunit;

namespace ReservasSaaS.UnitTests.Bookings;

public class BookingIdempotencyAndDuplicateTests
{
    private readonly Mock<IBookingRepository> _bookingRepoMock = new();
    private readonly Mock<ICompanyRepository> _companyRepoMock = new();
    private readonly Mock<IServiceRepository> _serviceRepoMock = new();
    private readonly Mock<IProfessionalRepository> _profRepoMock = new();
    private readonly Mock<IDateTimeProvider> _clockMock = new();

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _serviceId = Guid.NewGuid();
    private readonly Guid _professionalId = Guid.NewGuid();
    private readonly DateTime _fixedNow = new(2026, 10, 20, 10, 0, 0, DateTimeKind.Utc);

    public BookingIdempotencyAndDuplicateTests()
    {
        _clockMock.Setup(c => c.UtcNow).Returns(_fixedNow);

        var company = new Company(
            _companyId, 
            "Clinica Idempotencia", 
            "clinica-idem", 
            assignmentStrategy: ProfessionalAssignmentStrategy.ClientSelectsProfessional);

        _companyRepoMock.Setup(r => r.GetByIdAsync(_companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        var service = new Service(
            _serviceId, 
            _companyId, 
            "Odontología", 
            "Limpieza", 
            45, 
            80000m, 
            depositPolicy: DepositPolicy.None);

        _serviceRepoMock.Setup(r => r.GetByIdAsync(_companyId.ToString(), _serviceId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var professional = new Professional(
                id: _professionalId,
                companyId :_companyId,
                name: "Dr. Roberto Perez",
                email: "roberto@clinica.com",
                specialty : "Medico",
               assignedServiceIds :  new List<Guid> { _serviceId });

        // Tuesday schedule
        professional.SetSchedule(new List<WorkingSchedule>
        {
            new(DayOfWeek.Tuesday, new TimeOnly(8, 0), new TimeOnly(18, 0), true)
        });

        _profRepoMock.Setup(r => r.GetByIdAsync( _professionalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(professional);
    }

    [Fact]
    public async Task CreateBooking_WhenIdempotencyKeyAlreadyExists_ReturnsExistingBookingWithoutDuplicating()
    {
        // Arrange
        const string idempotencyKey = "client-req-uuid-12345";
        var existingBooking = new Booking(
            id: Guid.NewGuid(),
            companyId: _companyId,
            serviceId: _serviceId,
            serviceName: "Odontología",
            professionalId: _professionalId,
            professionalName: "Dr. Roberto Perez",
            customerName: "Carlos Cliente",
            customerEmail: "carlos@gmail.com",
            customerPhone: "+573001112233",
            scheduledStartTimeUtc: _fixedNow.AddHours(2),
            scheduledEndTimeUtc: _fixedNow.AddHours(2.75),
            totalServicePrice: 80000m,
            depositRequiredAmount: 0m,
            idempotencyKey: idempotencyKey);

        _bookingRepoMock.Setup(r => r.GetByIdempotencyKeyAsync(_companyId, idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBooking);

        var useCase = new CreateBookingUseCase(
            _bookingRepoMock.Object,
            _companyRepoMock.Object,
            _serviceRepoMock.Object,
            _profRepoMock.Object,
            _clockMock.Object,
            NullLogger<CreateBookingUseCase>.Instance);

        var command = new CreateBookingCommand(
            ServiceId: _serviceId,
            StartTimeUtc: _fixedNow.AddHours(2),
            CustomerName: "Carlos Cliente",
            CustomerPhone: "+573001112233",
            ProfessionalId: _professionalId,
            IdempotencyKey: idempotencyKey);

        // Act
        var result = await useCase.ExecuteAsync(_companyId, command);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existingBooking.Id);
        result.IdempotencyKey.Should().Be(idempotencyKey);

        // Verify repository CreateAsync was NEVER called again
        _bookingRepoMock.Verify(r => r.CreateAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateBooking_WhenIdempotencyKeyIsNew_CreatesNewBooking()
    {
        // Arrange
        const string idempotencyKey = "fresh-unique-req-789";
        _bookingRepoMock.Setup(r => r.GetByIdempotencyKeyAsync(_companyId, idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);

        _bookingRepoMock.Setup(r => r.GetOverlappingBookingsAsync(
            _companyId, _professionalId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking>());

        var useCase = new CreateBookingUseCase(
            _bookingRepoMock.Object,
            _companyRepoMock.Object,
            _serviceRepoMock.Object,
            _profRepoMock.Object,
            _clockMock.Object,
            NullLogger<CreateBookingUseCase>.Instance);

        var command = new CreateBookingCommand(
            ServiceId: _serviceId,
            StartTimeUtc: _fixedNow.AddHours(2),
            CustomerName: "Lucia Gomez",
            CustomerPhone: "+573009998877",
            ProfessionalId: _professionalId,
            IdempotencyKey: idempotencyKey);

        // Act
        var result = await useCase.ExecuteAsync(_companyId, command);

        // Assert
        result.Should().NotBeNull();
        result.CustomerName.Should().Be("Lucia Gomez");
        result.IdempotencyKey.Should().Be(idempotencyKey);

        _bookingRepoMock.Verify(r => r.CreateAsync(It.Is<Booking>(b => b.IdempotencyKey == idempotencyKey), It.IsAny<CancellationToken>()), Times.Once);
    }
}
