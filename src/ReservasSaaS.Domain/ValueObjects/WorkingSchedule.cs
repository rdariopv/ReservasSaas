using ReservasSaaS.Domain.Common;
using ReservasSaaS.Domain.Exceptions;

namespace ReservasSaaS.Domain.ValueObjects;

/// <summary>
/// Defines regular working hours for a professional on a specific day of the week.
/// </summary>
public class WorkingSchedule : ValueObject
{
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public bool IsEnabled { get; private set; }

    private WorkingSchedule()
    {
    }

    public WorkingSchedule(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, bool isEnabled = true)
    {
        if (isEnabled && startTime >= endTime)
        {
            throw new ValidationException($"Start time ({startTime}) must be earlier than end time ({endTime}) on {dayOfWeek}.");
        }

        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        IsEnabled = isEnabled;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DayOfWeek;
        yield return StartTime;
        yield return EndTime;
        yield return IsEnabled;
    }
}
