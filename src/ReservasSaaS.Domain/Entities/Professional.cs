using ReservasSaaS.Domain.Common;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.ValueObjects;

namespace ReservasSaaS.Domain.Entities;

/// <summary>
/// Professional aggregate root representing a service provider/staff member.
/// Linked to an authorized User identity when applicable.
/// Holds assigned services, working schedules, recurring breaks, and date-specific exceptions.
/// </summary>
public class Professional : BaseEntity<Guid>, IAggregateRoot
{
    public Guid CompanyId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Name { get; private set; }
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Specialty { get; private set; }
    public bool IsActive { get; private set; }

    public HashSet<Guid> AssignedServiceIds { get; private set; } = new();
    public List<WorkingSchedule> WorkingSchedules { get; private set; } = new();
    public List<BreakSchedule> BreakSchedules { get; private set; } = new();
    public List<ScheduleException> Exceptions { get; private set; } = new();

    private Professional()
    {
        Name = string.Empty;
    }

    public Professional(
        Guid id,
        Guid companyId,
        string name,
        Guid? userId = null,
        string? email = null,
        string? phoneNumber = null,
        string? specialty = null,
        bool isActive = true,
        IEnumerable<Guid>? assignedServiceIds = null,
        IEnumerable<WorkingSchedule>? workingSchedules = null,
        IEnumerable<BreakSchedule>? breakSchedules = null)
        : base(id)
    {
        if (companyId == Guid.Empty)
            throw new ValidationException("CompanyId cannot be empty.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Professional name cannot be empty.");

        CompanyId = companyId;
        UserId = userId;
        Name = name.Trim();
        Email = email?.Trim().ToLowerInvariant();
        PhoneNumber = phoneNumber?.Trim();
        Specialty = specialty?.Trim();
        IsActive = isActive;

        if (assignedServiceIds != null)
        {
            AssignedServiceIds = new HashSet<Guid>(assignedServiceIds);
        }

        if (workingSchedules != null)
        {
            WorkingSchedules = workingSchedules.ToList();
        }
        else
        {
            // Set sensible default: Monday to Friday 09:00 - 18:00
            SetDefaultWorkingHours();
        }

        if (breakSchedules != null)
        {
            BreakSchedules = breakSchedules.ToList();
        }
        else
        {
            // Default 1-hour lunch break 13:00 - 14:00
            SetDefaultBreakHours();
        }

        CreatedAtUtc = DateTime.UtcNow;
    }

    public void LinkToUser(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ValidationException("UserId cannot be empty.");

        UserId = userId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UnlinkUser()
    {
        UserId = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateProfile(string name, string? email, string? phoneNumber, string? specialty)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Professional name cannot be empty.");

        Name = name.Trim();
        Email = email?.Trim().ToLowerInvariant();
        PhoneNumber = phoneNumber?.Trim();
        Specialty = specialty?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AssignService(Guid serviceId)
    {
        if (serviceId == Guid.Empty)
            throw new ValidationException("ServiceId cannot be empty.");

        AssignedServiceIds.Add(serviceId);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RemoveService(Guid serviceId)
    {
        AssignedServiceIds.Remove(serviceId);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetAssignedServices(IEnumerable<Guid> serviceIds)
    {
        AssignedServiceIds = new HashSet<Guid>(serviceIds);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool CanProvideService(Guid serviceId)
    {
        return IsActive && AssignedServiceIds.Contains(serviceId);
    }

    public void SetSchedule(
        IEnumerable<WorkingSchedule> workingSchedules,
        IEnumerable<BreakSchedule>? breakSchedules = null,
        IEnumerable<ScheduleException>? exceptions = null)
    {
        WorkingSchedules = workingSchedules.ToList();

        if (breakSchedules != null)
        {
            BreakSchedules = breakSchedules.ToList();
        }

        if (exceptions != null)
        {
            Exceptions = exceptions.ToList();
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddException(ScheduleException exception)
    {
        Exceptions.RemoveAll(e => e.Date == exception.Date);
        Exceptions.Add(exception);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetDefaultWorkingHours()
    {
        WorkingSchedules = new List<WorkingSchedule>
        {
            new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(18, 0)),
            new(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(18, 0)),
            new(DayOfWeek.Wednesday, new TimeOnly(9, 0), new TimeOnly(18, 0)),
            new(DayOfWeek.Thursday, new TimeOnly(9, 0), new TimeOnly(18, 0)),
            new(DayOfWeek.Friday, new TimeOnly(9, 0), new TimeOnly(18, 0)),
            new(DayOfWeek.Saturday, new TimeOnly(9, 0), new TimeOnly(14, 0)),
            new(DayOfWeek.Sunday, new TimeOnly(9, 0), new TimeOnly(14, 0), isEnabled: false)
        };
    }

    private void SetDefaultBreakHours()
    {
        BreakSchedules = new List<BreakSchedule>
        {
            new(DayOfWeek.Monday, new TimeOnly(13, 0), new TimeOnly(14, 0), "Almuerzo"),
            new(DayOfWeek.Tuesday, new TimeOnly(13, 0), new TimeOnly(14, 0), "Almuerzo"),
            new(DayOfWeek.Wednesday, new TimeOnly(13, 0), new TimeOnly(14, 0), "Almuerzo"),
            new(DayOfWeek.Thursday, new TimeOnly(13, 0), new TimeOnly(14, 0), "Almuerzo"),
            new(DayOfWeek.Friday, new TimeOnly(13, 0), new TimeOnly(14, 0), "Almuerzo")
        };
    }
}
