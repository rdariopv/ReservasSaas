using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReservasSaaS.Application.DTOs.Bookings;
using ReservasSaaS.Application.UseCases.Bookings;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Domain.ValueObjects;
using System.Xml.Linq;
using Xunit;

namespace ReservasSaaS.UnitTests.Bookings;

public class BookingConcurrencyAndConflictTests
{
    private readonly Mock<IBookingRepository> _bookingRepoMock = new();
    private readonly Mock<ICompanyRepository> _companyRepoMock = new();
    private readonly Mock<IServiceRepository> _serviceRepoMock = new();
    private readonly Mock<IProfessionalRepository> _profRepoMock = new();
    private readonly Mock<IDateTimeProvider> _clockMock = new();

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _serviceId = Guid.NewGuid();
    private readonly Guid _professionalId = Guid.NewGuid();
    private readonly DateTime _tuesdayUtc = new(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);

    public BookingConcurrencyAndConflictTests()
    {
        _clockMock.Setup(c => c.UtcNow).Returns(_tuesdayUtc);
    }

    [Fact]
    public async Task CreateBooking_WhenSimultaneousOverlappingBookingExists_ThrowsConcurrencyException()
    {
        // Arrange: Client selects professional mode
        var company = new Company(
            _companyId, 
            "Barberia Concurrencia", 
            "barberia-conc", 
            assignmentStrategy: ProfessionalAssignmentStrategy.ClientSelectsProfessional);

        _companyRepoMock.Setup(r => r.GetByIdAsync(_companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        var service = new Service(
            _serviceId, 
            _companyId, 
            "Barba y Pelo", 
            "Servicio Completo", 
            45, 
            40000m, 
            depositPolicy: DepositPolicy.None);

        _serviceRepoMock.Setup(r => r.GetByIdAsync( _serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        //var professional = new Professional(
        //    _professionalId,
        //    _companyId,
        //    "Carlos",
        //    "Barbero",
        //    "carlos@barberia.com",
        //    new List<Guid> { _serviceId });

        var professional = new Professional(
                        id: _professionalId,
                        companyId: _companyId,
                        name: "Carlos",
                        email: "carlos@barberia.com",
                        specialty: "Barbero",
                        assignedServiceIds: new List<Guid> { _serviceId });

        professional.SetSchedule(new List<WorkingSchedule>
        {
            new(DayOfWeek.Tuesday, new TimeOnly(8, 0), new TimeOnly(18, 0), true)
        });

        _profRepoMock.Setup(r => r.GetByIdAsync(_professionalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(professional);

        // Simulate an existing overlapping booking active in the repository
        var existingBooking = new Booking(
            id: Guid.NewGuid(),
            companyId: _companyId,
            serviceId: _serviceId,
            serviceName: "Barba y Pelo",
            professionalId: _professionalId,
            professionalName: "Carlos Barbero",
            customerName: "Primer Cliente",
            customerEmail: "primero@test.com",
            customerPhone: "+573001112233",
            scheduledStartTimeUtc: _tuesdayUtc.AddHours(2), // 11:00 to 11:45
            scheduledEndTimeUtc: _tuesdayUtc.AddHours(2).AddMinutes(45),
            totalServicePrice: 40000m,
            depositRequiredAmount: 0m);

        _bookingRepoMock.Setup(r => r.GetOverlappingBookingsAsync(
            _companyId, _professionalId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { existingBooking });

        var useCase = new CreateBookingUseCase(
            _bookingRepoMock.Object,
            _companyRepoMock.Object,
            _serviceRepoMock.Object,
            _profRepoMock.Object,
            _clockMock.Object,
            NullLogger<CreateBookingUseCase>.Instance);

        var conflictingCommand = new CreateBookingCommand(
            ServiceId: _serviceId,
            StartTimeUtc: _tuesdayUtc.AddHours(2).AddMinutes(15), // 11:15 - Overlaps with 11:00-11:45!
            CustomerName: "Segundo Cliente Concurrente",
            CustomerPhone: "+573009998877",
            ProfessionalId: _professionalId);

        // Act
        var act = async () => await useCase.ExecuteAsync(_companyId, conflictingCommand);

        // Assert: Must reject and avoid double booking
        await act.Should().ThrowAsync<ConcurrencyException>()
            .WithMessage("*ya tiene una reserva en el horario seleccionado*");

        _bookingRepoMock.Verify(r => r.CreateAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateBooking_BusinessAssignsMode_WhenZeroCapacityAvailable_ThrowsValidationException()
    {
        // Arrange: Business assigns mode
        var company = new Company(
            _companyId, 
            "Clinica Sin Cupos", 
            "clinica-full", 
            assignmentStrategy: ProfessionalAssignmentStrategy.BusinessAssignsProfessional);

        _companyRepoMock.Setup(r => r.GetByIdAsync(_companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        var service = new Service(
            _serviceId, 
            _companyId, 
            "Consulta", 
            "General", 
            30, 
            50000m, 
            depositPolicy: DepositPolicy.None);

        _serviceRepoMock.Setup(r => r.GetByIdAsync(_companyId.ToString(), _serviceId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var singleProf = new Professional(
           id: _professionalId,
           companyId:  _companyId,
           name: "Dra. Unica",
           specialty: "Medica",
           email: "unica@clinica.com",
           assignedServiceIds: new List<Guid> { _serviceId });

        singleProf.SetSchedule(new List<WorkingSchedule>
        {
            new(DayOfWeek.Tuesday, new TimeOnly(8, 0), new TimeOnly(18, 0), true)
        });

        _profRepoMock.Setup(r => r.GetByCompanyIdAsync(_companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Professional> { singleProf });

        // Simulate single professional is busy at 10:00
        _bookingRepoMock.Setup(r => r.GetOverlappingBookingsAsync(
            _companyId, singleProf.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { 
                new Booking(Guid.NewGuid(), _companyId, _serviceId, "Consulta", singleProf.Id, "Dra. Unica", "Otro", "", "+57", _tuesdayUtc.AddHours(1), _tuesdayUtc.AddHours(1.5), 50000m, 0m) 
            });

        var useCase = new CreateBookingUseCase(
            _bookingRepoMock.Object,
            _companyRepoMock.Object,
            _serviceRepoMock.Object,
            _profRepoMock.Object,
            _clockMock.Object,
            NullLogger<CreateBookingUseCase>.Instance);

        var command = new CreateBookingCommand(
            ServiceId: _serviceId,
            StartTimeUtc: _tuesdayUtc.AddHours(1), // Exactly during busy slot
            CustomerName: "Esperando Cupo",
            CustomerPhone: "+573110001122",
            ProfessionalId: null); // Business assigns

        // Act
        var act = async () => await useCase.ExecuteAsync(_companyId, command);

        // Assert: Must refuse to confirm/create when capacity is zero
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*No hay capacidad real disponible en el horario solicitado*");
    }
}
