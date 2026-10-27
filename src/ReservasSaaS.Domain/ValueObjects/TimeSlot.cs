using ReservasSaaS.Domain.Common;

namespace ReservasSaaS.Domain.ValueObjects;

/// <summary>
/// Calculated available appointment slot ready for booking by Flutter or WhatsApp.
/// </summary>
public class TimeSlot : ValueObject
{
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }
    public int DurationMinutes { get; private set; }
    public Guid? ProfessionalId { get; private set; }
    public string? ProfessionalName { get; private set; }
    public bool IsAvailable { get; private set; }

    public TimeSlot(
        DateTime startUtc, 
        DateTime endUtc, 
        int durationMinutes, 
        Guid? professionalId = null, 
        string? professionalName = null, 
        bool isAvailable = true)
    {
        StartUtc = startUtc;
        EndUtc = endUtc;
        DurationMinutes = durationMinutes;
        ProfessionalId = professionalId;
        ProfessionalName = professionalName;
        IsAvailable = isAvailable;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartUtc;
        yield return EndUtc;
        yield return DurationMinutes;
        yield return ProfessionalId;
    }
}
