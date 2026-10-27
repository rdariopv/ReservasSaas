using Microsoft.Extensions.Logging;
using ReservasSaaS.Application.DTOs.Agenda;
using ReservasSaaS.Application.Ports.Incoming;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Domain.ValueObjects;

namespace ReservasSaaS.Application.UseCases.Agenda;

/// <summary>
/// Centralized availability engine for booking slots calculation.
/// Implements working hours, breaks, schedule exceptions, service duration, buffers, and existing bookings.
/// Shared across Flutter app and WhatsApp bot to guarantee no overbooking outside real capacity.
/// </summary>
public class GetAvailableSlotsUseCase : IGetAvailableSlotsUseCase
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IProfessionalRepository _professionalRepository;
    private readonly IExistingAppointmentsQueryPort _appointmentsPort;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<GetAvailableSlotsUseCase> _logger;

    public GetAvailableSlotsUseCase(
        ICompanyRepository companyRepository,
        IServiceRepository serviceRepository,
        IProfessionalRepository professionalRepository,
        IExistingAppointmentsQueryPort appointmentsPort,
        IDateTimeProvider dateTimeProvider,
        ILogger<GetAvailableSlotsUseCase> logger)
    {
        _companyRepository = companyRepository;
        _serviceRepository = serviceRepository;
        _professionalRepository = professionalRepository;
        _appointmentsPort = appointmentsPort;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<DayAvailabilityDto> ExecuteAsync(
        Guid companyId,
        Guid serviceId,
        DateOnly date,
        Guid? professionalId = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Fetch and validate Company
        var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);
        if (company == null || company.Status == CompanyStatus.Archived)
        {
            throw new EntityNotFoundException("Company", companyId);
        }

        if (company.Status == CompanyStatus.Suspended)
        {
            throw new ValidationException($"Company '{company.Name}' is currently suspended.");
        }

        // 2. Fetch and validate Service
        var service = await _serviceRepository.GetByIdAsync(serviceId, cancellationToken);
        if (service == null || service.CompanyId != companyId || service.Status != ServiceStatus.Active)
        {
            throw new EntityNotFoundException("Service", serviceId);
        }

        // 3. Resolve eligible professionals providing this service
        var eligibleProfessionals = new List<Professional>();

        if (professionalId.HasValue && professionalId.Value != Guid.Empty)
        {
            var requestedProf = await _professionalRepository.GetByIdAsync(professionalId.Value, cancellationToken);
            if (requestedProf == null || requestedProf.CompanyId != companyId || !requestedProf.CanProvideService(serviceId))
            {
                throw new ValidationException("The requested professional is not available for this service.");
            }
            eligibleProfessionals.Add(requestedProf);
        }
        else
        {
            var allCompanyProfessionals = await _professionalRepository.GetByServiceIdAsync(companyId, serviceId, cancellationToken);
            eligibleProfessionals.AddRange(allCompanyProfessionals.Where(p => p.IsActive));
        }

        if (eligibleProfessionals.Count == 0)
        {
            _logger.LogInformation("No active professionals found for service '{ServiceName}' on date {Date}", service.Name, date);
            return new DayAvailabilityDto(
                Date: date,
                CompanyId: company.Id,
                CompanyName: company.Name,
                TimeZone: company.TimeZone ?? "America/Bogota",
                AssignmentStrategy: company.AssignmentStrategy.ToString(),
                TotalSlotsAvailable: 0,
                Slots: Array.Empty<AvailableSlotDto>()
            );
        }

        // 4. Calculate available time slots across all eligible professionals
        var slotInterval = company.DefaultSlotIntervalMinutes > 0 ? company.DefaultSlotIntervalMinutes : 30;
        var depositAmount = service.DepositPolicy.CalculateDepositAmount(service.Price);
        var nowUtc = _dateTimeProvider.UtcNow;

        // Map to store slot candidates: Key = StartTimeLocal, Value = list of professionals available at that time
        var slotsMap = new SortedDictionary<TimeOnly, List<Professional>>();

        foreach (var professional in eligibleProfessionals)
        {
            var workingHours = GetEffectiveWorkingHours(professional, date);
            if (workingHours == null) continue; // Day off or not working

            var breaks = GetBreaksForDay(professional, date);
            var busyIntervals = await _appointmentsPort.GetBusyIntervalsAsync(companyId, professional.Id, date, cancellationToken);

            var candidateTime = workingHours.Value.Start;
            var dayEnd = workingHours.Value.End;

            while (candidateTime < dayEnd)
            {
                var slotStartLocal = candidateTime;
                var slotEndLocal = slotStartLocal.AddMinutes(service.DurationMinutes);

                // Service buffer boundaries
                var occupiedStartLocal = slotStartLocal.AddMinutes(-service.BufferBeforeMinutes);
                var occupiedEndLocal = slotEndLocal.AddMinutes(service.BufferAfterMinutes);

                // 1) Must fit within the professional's shift
                if (occupiedStartLocal < workingHours.Value.Start || occupiedEndLocal > dayEnd)
                {
                    candidateTime = candidateTime.AddMinutes(slotInterval);
                    continue;
                }

                // 2) Must not overlap with any break
                if (OverlapsWithAnyBreak(occupiedStartLocal, occupiedEndLocal, breaks))
                {
                    candidateTime = candidateTime.AddMinutes(slotInterval);
                    continue;
                }

                // 3) Convert to UTC estimate for date comparison and busy collision check
                var slotStartDateTimeUtc = date.ToDateTime(slotStartLocal, DateTimeKind.Utc);
                var slotEndDateTimeUtc = date.ToDateTime(slotEndLocal, DateTimeKind.Utc);
                var occupiedStartUtc = date.ToDateTime(occupiedStartLocal, DateTimeKind.Utc);
                var occupiedEndUtc = date.ToDateTime(occupiedEndLocal, DateTimeKind.Utc);

                // Check if slot is in the past
                if (slotStartDateTimeUtc <= nowUtc)
                {
                    candidateTime = candidateTime.AddMinutes(slotInterval);
                    continue;
                }

                // 4) Must not overlap with existing busy/booked intervals
                if (OverlapsWithAnyBooking(occupiedStartUtc, occupiedEndUtc, busyIntervals))
                {
                    candidateTime = candidateTime.AddMinutes(slotInterval);
                    continue;
                }

                // Slot is valid!
                if (!slotsMap.ContainsKey(slotStartLocal))
                {
                    slotsMap[slotStartLocal] = new List<Professional>();
                }
                slotsMap[slotStartLocal].Add(professional);

                candidateTime = candidateTime.AddMinutes(slotInterval);
            }
        }

        // 5. Construct final DTO response
        var resultSlots = new List<AvailableSlotDto>();

        foreach (var (time, availableProfs) in slotsMap)
        {
            var startUtc = date.ToDateTime(time, DateTimeKind.Utc);
            var endUtc = startUtc.AddMinutes(service.DurationMinutes);
            var endTimeLocal = time.AddMinutes(service.DurationMinutes);

            var profDtos = availableProfs
                .Select(p => new AvailableProfessionalDto(p.Id, p.Name))
                .ToList();

            // When BusinessAssignsProfessional is selected or single professional is requested:
            Guid? assignedProfId = null;
            string? assignedProfName = null;

            if (company.AssignmentStrategy == ProfessionalAssignmentStrategy.BusinessAssignsProfessional || professionalId.HasValue)
            {
                var assigned = availableProfs[0];
                assignedProfId = assigned.Id;
                assignedProfName = assigned.Name;
            }

            resultSlots.Add(new AvailableSlotDto(
                StartUtc: startUtc,
                EndUtc: endUtc,
                StartTimeLocal: time.ToString("HH:mm"),
                EndTimeLocal: endTimeLocal.ToString("HH:mm"),
                ServiceId: service.Id,
                ServiceName: service.Name,
                DurationMinutes: service.DurationMinutes,
                Price: service.Price,
                Currency: service.Currency,
                RequiresDeposit: service.DepositPolicy.RequiresDeposit,
                DepositAmount: depositAmount,
                DepositDescription: service.DepositPolicy.Description,
                AssignedProfessionalId: assignedProfId,
                AssignedProfessionalName: assignedProfName,
                AvailableProfessionals: profDtos
            ));
        }

        return new DayAvailabilityDto(
            Date: date,
            CompanyId: company.Id,
            CompanyName: company.Name,
            TimeZone: company.TimeZone ?? "America/Bogota",
            AssignmentStrategy: company.AssignmentStrategy.ToString(),
            TotalSlotsAvailable: resultSlots.Count,
            Slots: resultSlots
        );
    }

    private static (TimeOnly Start, TimeOnly End)? GetEffectiveWorkingHours(Professional professional, DateOnly date)
    {
        // 1. Check date-specific exception (holidays, vacations, custom hours)
        var exception = professional.Exceptions.FirstOrDefault(e => e.Date == date);
        if (exception != null)
        {
            if (exception.IsDayOff || !exception.StartTime.HasValue || !exception.EndTime.HasValue)
            {
                return null; // Day off
            }

            return (exception.StartTime.Value, exception.EndTime.Value);
        }

        // 2. Check regular weekly schedule
        var regular = professional.WorkingSchedules.FirstOrDefault(w => w.DayOfWeek == date.DayOfWeek);
        if (regular == null || !regular.IsEnabled)
        {
            return null; // Closed / Day off
        }

        return (regular.StartTime, regular.EndTime);
    }

    private static List<BreakSchedule> GetBreaksForDay(Professional professional, DateOnly date)
    {
        return professional.BreakSchedules
            .Where(b => b.DayOfWeek == date.DayOfWeek)
            .ToList();
    }

    private static bool OverlapsWithAnyBreak(TimeOnly start, TimeOnly end, List<BreakSchedule> breaks)
    {
        foreach (var b in breaks)
        {
            // Two time intervals overlap if StartA < EndB and EndA > StartB
            if (start < b.EndTime && end > b.StartTime)
            {
                return true;
            }
        }
        return false;
    }

    private static bool OverlapsWithAnyBooking(DateTime startUtc, DateTime endUtc, IReadOnlyList<TimeInterval> busyList)
    {
        foreach (var busy in busyList)
        {
            if (startUtc < busy.EndUtc && endUtc > busy.StartUtc)
            {
                return true;
            }
        }
        return false;
    }
}
