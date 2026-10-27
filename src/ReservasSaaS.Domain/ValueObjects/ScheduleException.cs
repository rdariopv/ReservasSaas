using ReservasSaaS.Domain.Common;
using ReservasSaaS.Domain.Exceptions;

namespace ReservasSaaS.Domain.ValueObjects;

/// <summary>
/// Specific date override (vacations, public holidays, or custom working hours for a particular day).
/// </summary>
public class ScheduleException : ValueObject
{
    public DateOnly Date { get; private set; }
    public bool IsDayOff { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    public string Reason { get; private set; }

    private ScheduleException()
    {
        Reason = string.Empty;
    }

    public ScheduleException(DateOnly date, bool isDayOff, TimeOnly? startTime = null, TimeOnly? endTime = null, string reason = "")
    {
        if (!isDayOff)
        {
            if (!startTime.HasValue || !endTime.HasValue)
                throw new ValidationException("Start and End times are required when an exception is not a full day off.");

            if (startTime.Value >= endTime.Value)
                throw new ValidationException($"Exception start time ({startTime}) must be earlier than end time ({endTime}).");
        }

        Date = date;
        IsDayOff = isDayOff;
        StartTime = isDayOff ? null : startTime;
        EndTime = isDayOff ? null : endTime;
        Reason = string.IsNullOrWhiteSpace(reason) ? (isDayOff ? "Día libre" : "Horario especial") : reason.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Date;
        yield return IsDayOff;
        yield return StartTime;
        yield return EndTime;
        yield return Reason;
    }
}
