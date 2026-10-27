using ReservasSaaS.Domain.Common;
using ReservasSaaS.Domain.Exceptions;

namespace ReservasSaaS.Domain.ValueObjects;

/// <summary>
/// Defines a recurring break (e.g. lunch, rest interval) during a working day.
/// </summary>
public class BreakSchedule : ValueObject
{
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public string Name { get; private set; }

    private BreakSchedule()
    {
        Name = string.Empty;
    }

    public BreakSchedule(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, string name = "Descanso")
    {
        if (startTime >= endTime)
        {
            throw new ValidationException($"Break start time ({startTime}) must be earlier than end time ({endTime}) on {dayOfWeek}.");
        }

        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        Name = string.IsNullOrWhiteSpace(name) ? "Descanso" : name.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DayOfWeek;
        yield return StartTime;
        yield return EndTime;
        yield return Name;
    }
}
