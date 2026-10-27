using Microsoft.Extensions.Logging;
using ReservasSaaS.Application.DTOs.Bookings;
using ReservasSaaS.Application.Ports.Incoming;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.Ports.Outgoing;

namespace ReservasSaaS.Application.UseCases.Bookings;

/// <summary>
/// Handles atomic booking creation with idempotency, availability revalidation,
/// multi-modal assignment checks, and deposit policy enforcement.
/// </summary>
public class CreateBookingUseCase : ICreateBookingUseCase
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IProfessionalRepository _professionalRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CreateBookingUseCase> _logger;

    public CreateBookingUseCase(
        IBookingRepository bookingRepository,
        ICompanyRepository companyRepository,
        IServiceRepository serviceRepository,
        IProfessionalRepository professionalRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<CreateBookingUseCase> logger)
    {
        _bookingRepository = bookingRepository;
        _companyRepository = companyRepository;
        _serviceRepository = serviceRepository;
        _professionalRepository = professionalRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<BookingDto> ExecuteAsync(
        Guid companyId, 
        CreateBookingCommand command, 
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;

        // 1. Idempotency Check: Return existing booking if key is provided and already processed
        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            var existing = await _bookingRepository.GetByIdempotencyKeyAsync(
                companyId, 
                command.IdempotencyKey.Trim(), 
                cancellationToken);

            if (existing != null)
            {
                _logger.LogInformation(
                    "Idempotent booking request detected for Company '{CompanyId}' with key '{IdempotencyKey}'. Returning existing booking '{BookingId}'.",
                    companyId, command.IdempotencyKey, existing.Id);

                return BookingDto.FromDomain(existing);
            }
        }

        // 2. Tenant validation
        var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);
        if (company == null)
            throw new EntityNotFoundException(nameof(Company), companyId);

        if (company.Status != CompanyStatus.Active)
            throw new ValidationException($"Company '{company.Name}' is not active for new bookings.");

        // 3. Service validation
        var service = await _serviceRepository.GetByIdAsync(companyId.ToString(), command.ServiceId.ToString(), cancellationToken);
        if (service == null)
            throw new EntityNotFoundException(nameof(Service), command.ServiceId);

        if (service.Status != ServiceStatus.Active)
            throw new ValidationException($"Service '{service.Name}' is not currently active.");

        var startTimeUtc = command.StartTimeUtc;
        var endTimeUtc = startTimeUtc.AddMinutes(service.DurationMinutes);

        if (startTimeUtc < nowUtc.AddMinutes(-5))
            throw new ValidationException("Cannot book an appointment in the past.");

        Guid? assignedProfessionalId = null;
        string? assignedProfessionalName = null;

        // 4. Modality and Capacity Revalidation
        if (company.AssignmentStrategy == ProfessionalAssignmentStrategy.ClientSelectsProfessional)
        {
            if (!command.ProfessionalId.HasValue || command.ProfessionalId == Guid.Empty)
                throw new ValidationException("Under 'ClientSelectsProfessional' modality, a professional must be selected.");

            var professional = await _professionalRepository.GetByUserIdAsync(companyId, command.ProfessionalId.Value, cancellationToken);
            if (professional == null)
                throw new EntityNotFoundException(nameof(Professional), command.ProfessionalId.Value);

            if (!professional.IsActive)
                throw new ValidationException($"Professional '{professional.Name}' is not currently active.");

            if (!professional.CanProvideService(service.Id))
                throw new ValidationException($"Professional '{professional.Name}' is not enabled to perform service '{service.Name}'.");

            // Revalidate working hours, breaks, and exceptions
            ValidateProfessionalWorkingCapacity(professional, startTimeUtc, endTimeUtc);

            // Revalidate concurrency & overlapping bookings
            var overlaps = await _bookingRepository.GetOverlappingBookingsAsync(
                companyId, 
                professional.Id, 
                startTimeUtc, 
                endTimeUtc, 
                excludeBookingId: null, 
                cancellationToken);

            if (overlaps.Count > 0)
            {
                throw new ConcurrencyException(
                    $"El profesional '{professional.Name}' ya tiene una reserva en el horario seleccionado ({startTimeUtc:HH:mm} - {endTimeUtc:HH:mm} UTC).");
            }

            assignedProfessionalId = professional.Id;
            assignedProfessionalName = professional.Name;
        }
        else // BusinessAssignsProfessional
        {
            if (command.ProfessionalId.HasValue && command.ProfessionalId != Guid.Empty)
            {
                var professional = await _professionalRepository.GetByUserIdAsync (companyId, command.ProfessionalId.Value, cancellationToken);
                if (professional != null && professional.IsActive && professional.CanProvideService(service.Id))
                {
                    ValidateProfessionalWorkingCapacity(professional, startTimeUtc, endTimeUtc);

                    var overlaps = await _bookingRepository.GetOverlappingBookingsAsync(
                        companyId, professional.Id, startTimeUtc, endTimeUtc, null, cancellationToken);

                    if (overlaps.Count == 0)
                    {
                        assignedProfessionalId = professional.Id;
                        assignedProfessionalName = professional.Name;
                    }
                }
            }

            // If not directly assigned or requested, verify collective capacity
            if (!assignedProfessionalId.HasValue)
            {
                var allProfessionals = await _professionalRepository.GetByCompanyIdAsync(companyId, cancellationToken);
                var qualifiedProfessionals = allProfessionals
                    .Where(p => p.IsActive && p.CanProvideService(service.Id))
                    .ToList();

                if (qualifiedProfessionals.Count == 0)
                    throw new ValidationException($"No active professionals in the company are qualified for service '{service.Name}'.");

                bool hasAnyAvailableCapacity = false;
                foreach (var candidate in qualifiedProfessionals)
                {
                    try
                    {
                        ValidateProfessionalWorkingCapacity(candidate, startTimeUtc, endTimeUtc);

                        var overlaps = await _bookingRepository.GetOverlappingBookingsAsync(
                            companyId, candidate.Id, startTimeUtc, endTimeUtc, null, cancellationToken);

                        if (overlaps.Count == 0)
                        {
                            hasAnyAvailableCapacity = true;
                            break;
                        }
                    }
                    catch (ValidationException)
                    {
                        // Candidate not working or on break at this time; check next
                    }
                }

                if (!hasAnyAvailableCapacity)
                {
                    throw new ValidationException(
                        "No hay capacidad real disponible en el horario solicitado según las jornadas y reservas existentes.");
                }
            }
        }

        // 5. Calculate deposit
        var depositAmount = service.DepositPolicy.CalculateDepositAmount(service.Price);

        // 6. Create Booking entity
        var booking = new Booking(
            id: Guid.NewGuid(),
            companyId: companyId,
            serviceId: service.Id,
            serviceName: service.Name,
            professionalId: assignedProfessionalId,
            professionalName: assignedProfessionalName,
            customerName: command.CustomerName,
            customerEmail: command.CustomerEmail ?? string.Empty,
            customerPhone: command.CustomerPhone,
            scheduledStartTimeUtc: startTimeUtc,
            scheduledEndTimeUtc: endTimeUtc,
            totalServicePrice: service.Price,
            depositRequiredAmount: depositAmount,
            currency: service.Currency,
            notes: command.Notes,
            idempotencyKey: command.IdempotencyKey,
            expirationWindow: TimeSpan.FromMinutes(30),
            utcNow: nowUtc);

        // 7. Persist atomically
        await _bookingRepository.CreateAsync(booking, cancellationToken);

        _logger.LogInformation(
            "Booking '{BookingId}' created successfully for Company '{CompanyId}' with status '{Status}' and payment status '{PaymentStatus}'.",
            booking.Id, companyId, booking.Status, booking.PaymentStatus);

        return BookingDto.FromDomain(booking);
    }

    private static void ValidateProfessionalWorkingCapacity(
        Professional professional, 
        DateTime startUtc, 
        DateTime endUtc)
    {
        var date = DateOnly.FromDateTime(startUtc);
        var dayOfWeek = date.DayOfWeek;
        var startSlot = TimeOnly.FromDateTime(startUtc);
        var endSlot = TimeOnly.FromDateTime(endUtc);

        // Check date exceptions
        var exception = professional.Exceptions.FirstOrDefault(e => e.Date == date);
        if (exception != null )
        {
            throw new ValidationException(
                $"El profesional '{professional.Name}' no está disponible el {date:yyyy-MM-dd}: {exception.Reason}");
        }

        // Check working schedule
        var schedule = professional.WorkingSchedules.FirstOrDefault(s => s.DayOfWeek == dayOfWeek && s.IsEnabled);
        if (schedule == null)
        {
            throw new ValidationException(
                $"El profesional '{professional.Name}' no tiene jornada laboral asignada para los días {dayOfWeek}.");
        }

        if (startSlot < schedule.StartTime || endSlot > schedule.EndTime)
        {
            throw new ValidationException(
                $"El horario ({startSlot:HH:mm}-{endSlot:HH:mm}) excede la jornada del profesional ({schedule.StartTime:HH:mm}-{schedule.EndTime:HH:mm}).");
        }

        // Check breaks
        var conflictBreak = professional.BreakSchedules.FirstOrDefault(b => 
            b.DayOfWeek == dayOfWeek && 
            startSlot < b.EndTime && 
            endSlot > b.StartTime);

        if (conflictBreak != null)
        {
            throw new ValidationException(
                $"El horario solicitado cruza con el receso del profesional '{conflictBreak.Name}' ({conflictBreak.StartTime:HH:mm}-{conflictBreak.EndTime:HH:mm}).");
        }
    }
}
