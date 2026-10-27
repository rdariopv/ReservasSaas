using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReservasSaaS.Application.UseCases.Agenda;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Domain.ValueObjects;
using Xunit;

namespace ReservasSaaS.UnitTests.Agenda;

public class AvailabilityEngineTests
{
    private readonly Mock<ICompanyRepository> _companyRepoMock;
    private readonly Mock<IServiceRepository> _serviceRepoMock;
    private readonly Mock<IProfessionalRepository> _professionalRepoMock;
    private readonly Mock<IExistingAppointmentsQueryPort> _appointmentsPortMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly GetAvailableSlotsUseCase _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _serviceId = Guid.NewGuid();
    private readonly Guid _profId = Guid.NewGuid();

    private readonly Company _company;
    private readonly Service _service;
    private readonly Professional _professional;
    private readonly DateOnly _targetMonday = new(2026, 10, 19); // A Monday

    public AvailabilityEngineTests()
    {
        _companyRepoMock = new Mock<ICompanyRepository>();
        _serviceRepoMock = new Mock<IServiceRepository>();
        _professionalRepoMock = new Mock<IProfessionalRepository>();
        _appointmentsPortMock = new Mock<IExistingAppointmentsQueryPort>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();

        // Set fixed current time in the past relative to the Monday test date
        _dateTimeProviderMock.Setup(d => d.UtcNow).Returns(new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc));

        _company = new Company(
            _companyId,
            "Barbería Estilo",
            "barberia-estilo",
            assignmentStrategy: ProfessionalAssignmentStrategy.ClientSelectsProfessional,
            defaultSlotIntervalMinutes: 30);

        // 30 min haircut, 10 min buffer after, $25 COP
        _service = new Service(
            _serviceId,
            _companyId,
            "Corte Clásico",
            "Corte de cabello masculino",
            durationMinutes: 30,
            price: 25000,
            bufferBeforeMinutes: 0,
            bufferAfterMinutes: 10,
            depositPolicy: new DepositPolicy(true, DepositType.Percentage, 20, "20% anticipo requerido")
        );

        // Working Monday 09:00 - 18:00, lunch break 13:00 - 14:00
        var workingHours = new List<WorkingSchedule>
        {
            new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(18, 0), isEnabled: true)
        };

        var breaks = new List<BreakSchedule>
        {
            new(DayOfWeek.Monday, new TimeOnly(13, 0), new TimeOnly(14, 0), "Almuerzo")
        };

        _professional = new Professional(
            _profId,
            _companyId,
            "Carlos Barbero",
            assignedServiceIds: new[] { _serviceId },
            workingSchedules: workingHours,
            breakSchedules: breaks
        );

        _companyRepoMock.Setup(r => r.GetByIdAsync(_companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_company);

        _serviceRepoMock.Setup(r => r.GetByIdAsync(_serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_service);

        _professionalRepoMock.Setup(r => r.GetByIdAsync(_profId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_professional);

        _professionalRepoMock.Setup(r => r.GetByServiceIdAsync(_companyId, _serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Professional> { _professional });

        // Default: no existing busy bookings
        _appointmentsPortMock.Setup(p => p.GetBusyIntervalsAsync(_companyId, _profId, _targetMonday, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeInterval>());

        _sut = new GetAvailableSlotsUseCase(
            _companyRepoMock.Object,
            _serviceRepoMock.Object,
            _professionalRepoMock.Object,
            _appointmentsPortMock.Object,
            _dateTimeProviderMock.Object,
            NullLogger<GetAvailableSlotsUseCase>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_GeneratesSlotsWithinWorkingHours_ExcludingLunchBreak()
    {
        // Act
        var result = await _sut.ExecuteAsync(_companyId, _serviceId, _targetMonday, _profId);

        // Assert
        result.Should().NotBeNull();
        result.TotalSlotsAvailable.Should().BeGreaterThan(0);
        result.Slots.Should().NotBeEmpty();

        // 1. Should have slots in the morning (e.g. 09:00, 09:30, 10:00...)
        result.Slots.Should().Contain(s => s.StartTimeLocal == "09:00");
        result.Slots.Should().Contain(s => s.StartTimeLocal == "09:30");

        // 2. Lunch break is 13:00 - 14:00.
        // A 30-min slot with 10-min after-buffer at 12:30 ends at 13:00 (buffer extends to 13:10 -> overlaps with 13:00-14:00 lunch break!)
        // Thus 12:30 must be excluded because the 10-min buffer intersects lunch!
        result.Slots.Should().NotContain(s => s.StartTimeLocal == "13:00");
        result.Slots.Should().NotContain(s => s.StartTimeLocal == "13:30");

        // 3. First afternoon slot after lunch: 14:00 is available
        result.Slots.Should().Contain(s => s.StartTimeLocal == "14:00");

        // 4. Deposit calculation should be 20% of 25000 = 5000
        var firstSlot = result.Slots.First();
        firstSlot.RequiresDeposit.Should().BeTrue();
        firstSlot.DepositAmount.Should().Be(5000m);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExistingBookingPresent_ExcludesOccupiedIntervalToPreventDoubleBooking()
    {
        // Arrange: Carlos already has an appointment from 10:00 to 11:00 UTC
        var bookedStart = _targetMonday.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);
        var bookedEnd = _targetMonday.ToDateTime(new TimeOnly(11, 0), DateTimeKind.Utc);

        var busyIntervals = new List<TimeInterval>
        {
            new(bookedStart, bookedEnd, "APPT-1001")
        };

        _appointmentsPortMock.Setup(p => p.GetBusyIntervalsAsync(_companyId, _profId, _targetMonday, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busyIntervals);

        // Act
        var result = await _sut.ExecuteAsync(_companyId, _serviceId, _targetMonday, _profId);

        // Assert
        // Slots at 10:00 and 10:30 MUST be excluded due to existing reservation!
        result.Slots.Should().NotContain(s => s.StartTimeLocal == "10:00");
        result.Slots.Should().NotContain(s => s.StartTimeLocal == "10:30");

        // 09:00 is free (ends at 09:30 + 10m buffer = 09:40 < 10:00)
        result.Slots.Should().Contain(s => s.StartTimeLocal == "09:00");

        // 11:00 is free (starts right after busy interval)
        result.Slots.Should().Contain(s => s.StartTimeLocal == "11:00");
    }

    [Fact]
    public async Task ExecuteAsync_WhenScheduleExceptionIsDayOff_ReturnsZeroSlots()
    {
        // Arrange: Add a day off exception for the target Monday (e.g. Bank Holiday)
        _professional.AddException(new ScheduleException(_targetMonday, isDayOff: true, reason: "Día Festivo"));

        // Act
        var result = await _sut.ExecuteAsync(_companyId, _serviceId, _targetMonday, _profId);

        // Assert
        result.TotalSlotsAvailable.Should().Be(0);
        result.Slots.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCompanyConfiguredForBusinessAssignsProfessional_AutoAssignsAvailableProfessional()
    {
        // Arrange: Switch strategy to BusinessAssignsProfessional
        _company.SetBookingSettings(ProfessionalAssignmentStrategy.BusinessAssignsProfessional, 30);

        // Act (client sends professionalId = null)
        var result = await _sut.ExecuteAsync(_companyId, _serviceId, _targetMonday, professionalId: null);

        // Assert
        result.AssignmentStrategy.Should().Be("BusinessAssignsProfessional");
        result.Slots.Should().NotBeEmpty();

        // Each slot must have an assigned professional auto-populated by the engine
        foreach (var slot in result.Slots)
        {
            slot.AssignedProfessionalId.Should().Be(_profId);
            slot.AssignedProfessionalName.Should().Be("Carlos Barbero");
        }
    }
}
