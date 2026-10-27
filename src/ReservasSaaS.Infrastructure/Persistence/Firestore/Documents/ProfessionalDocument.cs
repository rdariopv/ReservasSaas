using Google.Cloud.Firestore;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Abstractions;

namespace ReservasSaaS.Infrastructure.Persistence.Firestore.Documents;

[FirestoreData]
public class WorkingScheduleItem
{
    [FirestoreProperty]
    public string DayOfWeek { get; set; } = string.Empty;

    [FirestoreProperty]
    public string StartTime { get; set; } = string.Empty;

    [FirestoreProperty]
    public string EndTime { get; set; } = string.Empty;

    [FirestoreProperty]
    public bool IsEnabled { get; set; }
}

[FirestoreData]
public class BreakScheduleItem
{
    [FirestoreProperty]
    public string DayOfWeek { get; set; } = string.Empty;

    [FirestoreProperty]
    public string StartTime { get; set; } = string.Empty;

    [FirestoreProperty]
    public string EndTime { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;
}

[FirestoreData]
public class ScheduleExceptionItem
{
    [FirestoreProperty]
    public string Date { get; set; } = string.Empty;

    [FirestoreProperty]
    public bool IsDayOff { get; set; }

    [FirestoreProperty]
    public string? StartTime { get; set; }

    [FirestoreProperty]
    public string? EndTime { get; set; }

    [FirestoreProperty]
    public string Reason { get; set; } = string.Empty;
}

[FirestoreData]
public class ProfessionalDocument : IFirestoreDocument
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty]
    public string CompanyId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string? UserId { get; set; }

    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty]
    public string? Email { get; set; }

    [FirestoreProperty]
    public string? PhoneNumber { get; set; }

    [FirestoreProperty]
    public string? Specialty { get; set; }

    [FirestoreProperty]
    public bool IsActive { get; set; } = true;

    [FirestoreProperty]
    public List<string> AssignedServiceIds { get; set; } = new();

    [FirestoreProperty]
    public List<WorkingScheduleItem> WorkingSchedules { get; set; } = new();

    [FirestoreProperty]
    public List<BreakScheduleItem> BreakSchedules { get; set; } = new();

    [FirestoreProperty]
    public List<ScheduleExceptionItem> Exceptions { get; set; } = new();

    [FirestoreProperty]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [FirestoreProperty]
    public DateTime? UpdatedAtUtc { get; set; }
}
