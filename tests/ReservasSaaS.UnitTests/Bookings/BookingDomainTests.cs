using FluentAssertions;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;
using Xunit;

namespace ReservasSaaS.UnitTests.Bookings;

public class BookingDomainTests
{
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _serviceId = Guid.NewGuid();
    private readonly Guid _professionalId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithoutProfessional_InitializesInPendingAssignment()
    {
        // Act
        var booking = new Booking(
            id: Guid.NewGuid(),
            companyId: _companyId,
            serviceId: _serviceId,
            serviceName: "Corte de Cabello",
            professionalId: null,
            professionalName: null,
            customerName: "Juan Perez",
            customerEmail: "juan@example.com",
            customerPhone: "+573001234567",
            scheduledStartTimeUtc: DateTime.UtcNow.AddHours(2),
            scheduledEndTimeUtc: DateTime.UtcNow.AddHours(2.5),
            totalServicePrice: 50000m,
            depositRequiredAmount: 0m);

        // Assert
        booking.Status.Should().Be(BookingStatus.PendingAssignment);
        booking.PaymentStatus.Should().Be(PaymentStatus.NotRequired);
        booking.ExpiresAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithProfessionalAndDeposit_InitializesInPendingPayment()
    {
        // Act
        var booking = new Booking(
            id: Guid.NewGuid(),
            companyId: _companyId,
            serviceId: _serviceId,
            serviceName: "Tratamiento Facial",
            professionalId: _professionalId,
            professionalName: "Dra. Gomez",
            customerName: "Maria Lopez",
            customerEmail: "maria@example.com",
            customerPhone: "+573009876543",
            scheduledStartTimeUtc: DateTime.UtcNow.AddHours(3),
            scheduledEndTimeUtc: DateTime.UtcNow.AddHours(4),
            totalServicePrice: 120000m,
            depositRequiredAmount: 30000m);

        // Assert
        booking.Status.Should().Be(BookingStatus.PendingPayment);
        booking.PaymentStatus.Should().Be(PaymentStatus.Pending);
        booking.ExpiresAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithProfessionalAndNoDeposit_InitializesInConfirmed()
    {
        // Act
        var booking = new Booking(
            id: Guid.NewGuid(),
            companyId: _companyId,
            serviceId: _serviceId,
            serviceName: "Consulta General",
            professionalId: _professionalId,
            professionalName: "Dr. Ramirez",
            customerName: "Carlos Vega",
            customerEmail: "carlos@example.com",
            customerPhone: "+573105558888",
            scheduledStartTimeUtc: DateTime.UtcNow.AddHours(1),
            scheduledEndTimeUtc: DateTime.UtcNow.AddHours(1.5),
            totalServicePrice: 60000m,
            depositRequiredAmount: 0m);

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.PaymentStatus.Should().Be(PaymentStatus.NotRequired);
    }

    [Fact]
    public void PaymentStatus_IsKeptDistinct_FromBookingStatus()
    {
        // Arrange
        var booking = new Booking(
            id: Guid.NewGuid(),
            companyId: _companyId,
            serviceId: _serviceId,
            serviceName: "Masaje Terapéutico",
            professionalId: _professionalId,
            professionalName: "Ana Soto",
            customerName: "Sofia Ruiz",
            customerEmail: "sofia@example.com",
            customerPhone: "+573123456789",
            scheduledStartTimeUtc: DateTime.UtcNow.AddHours(5),
            scheduledEndTimeUtc: DateTime.UtcNow.AddHours(6),
            totalServicePrice: 80000m,
            depositRequiredAmount: 20000m);

        // Act 1: Client submits voucher
        booking.UpdatePayment(PaymentStatus.SubmittedForReview, 20000m, DateTime.UtcNow);

        // Assert 1
        booking.PaymentStatus.Should().Be(PaymentStatus.SubmittedForReview);
        booking.Status.Should().Be(BookingStatus.PaymentReview);

        // Act 2: Payment approved
        booking.UpdatePayment(PaymentStatus.DepositPaid, 20000m, DateTime.UtcNow);

        // Assert 2
        booking.PaymentStatus.Should().Be(PaymentStatus.DepositPaid);
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public void Confirm_WithoutAssignedProfessional_ThrowsValidationException()
    {
        // Arrange
        var booking = new Booking(
            id: Guid.NewGuid(),
            companyId: _companyId,
            serviceId: _serviceId,
            serviceName: "Corte",
            professionalId: null,
            professionalName: null,
            customerName: "Pedro Diaz",
            customerEmail: "pedro@example.com",
            customerPhone: "+573001112233",
            scheduledStartTimeUtc: DateTime.UtcNow.AddHours(2),
            scheduledEndTimeUtc: DateTime.UtcNow.AddHours(2.5),
            totalServicePrice: 40000m,
            depositRequiredAmount: 0m);

        // Act
        var act = () => booking.Confirm(DateTime.UtcNow);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*without an assigned professional*");
    }

    [Fact]
    public void Confirm_WhenDepositRequiredButNotPaid_ThrowsValidationException()
    {
        // Arrange
        var booking = new Booking(
            id: Guid.NewGuid(),
            companyId: _companyId,
            serviceId: _serviceId,
            serviceName: "Microblading",
            professionalId: _professionalId,
            professionalName: "Laura Martinez",
            customerName: "Diana Mora",
            customerEmail: "diana@example.com",
            customerPhone: "+573004445566",
            scheduledStartTimeUtc: DateTime.UtcNow.AddHours(4),
            scheduledEndTimeUtc: DateTime.UtcNow.AddHours(5),
            totalServicePrice: 200000m,
            depositRequiredAmount: 50000m);

        // Act
        var act = () => booking.Confirm(DateTime.UtcNow);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*deposit has not been paid*");
    }

    [Fact]
    public void ServiceExecutionLifecycle_CheckIn_Start_Complete_Succeeds()
    {
        // Arrange: Confirmed booking
        var booking = new Booking(
            id: Guid.NewGuid(),
            companyId: _companyId,
            serviceId: _serviceId,
            serviceName: "Fisioterapia",
            professionalId: _professionalId,
            professionalName: "Lic. Torres",
            customerName: "Andres Gil",
            customerEmail: "andres@example.com",
            customerPhone: "+573151234567",
            scheduledStartTimeUtc: DateTime.UtcNow.AddMinutes(10),
            scheduledEndTimeUtc: DateTime.UtcNow.AddMinutes(55),
            totalServicePrice: 90000m,
            depositRequiredAmount: 0m);

        var now = DateTime.UtcNow;

        // Step 1: Check In
        booking.CheckIn(now);
        booking.Status.Should().Be(BookingStatus.CheckedIn);

        // Step 2: Start Service
        booking.StartService(now.AddMinutes(2));
        booking.Status.Should().Be(BookingStatus.InProgress);

        // Step 3: Complete Service
        booking.CompleteService(now.AddMinutes(45));
        booking.Status.Should().Be(BookingStatus.Completed);
    }

    [Fact]
    public void CannotRescheduleOrCancel_CompletedBooking()
    {
        // Arrange
        var booking = new Booking(
            id: Guid.NewGuid(),
            companyId: _companyId,
            serviceId: _serviceId,
            serviceName: "Spa",
            professionalId: _professionalId,
            professionalName: "Carolina",
            customerName: "Elena",
            customerEmail: "elena@example.com",
            customerPhone: "+573007778899",
            scheduledStartTimeUtc: DateTime.UtcNow.AddHours(1),
            scheduledEndTimeUtc: DateTime.UtcNow.AddHours(2),
            totalServicePrice: 150000m,
            depositRequiredAmount: 0m);

        booking.CompleteService(DateTime.UtcNow);

        // Act & Assert
        var cancelAct = () => booking.Cancel("Ya no deseo", DateTime.UtcNow);
        cancelAct.Should().Throw<ValidationException>().WithMessage("*already completed*");

        var rescheduleAct = () => booking.Reschedule(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(1), null, null, DateTime.UtcNow);
        rescheduleAct.Should().Throw<ValidationException>().WithMessage("*Cannot reschedule*");
    }

    [Fact]
    public void MarkNoShow_WhenConfirmedOrCheckedIn_TransitionsSuccessfully()
    {
        // Arrange
        var booking = new Booking(
            id: Guid.NewGuid(),
            companyId: _companyId,
            serviceId: _serviceId,
            serviceName: "Nutrición",
            professionalId: _professionalId,
            professionalName: "Dra. Gomez",
            customerName: "Esteban",
            customerEmail: "esteban@example.com",
            customerPhone: "+573002223344",
            scheduledStartTimeUtc: DateTime.UtcNow.AddHours(1),
            scheduledEndTimeUtc: DateTime.UtcNow.AddHours(1.5),
            totalServicePrice: 70000m,
            depositRequiredAmount: 0m);

        // Act
        booking.MarkNoShow(DateTime.UtcNow);

        // Assert
        booking.Status.Should().Be(BookingStatus.NoShow);
    }
}
