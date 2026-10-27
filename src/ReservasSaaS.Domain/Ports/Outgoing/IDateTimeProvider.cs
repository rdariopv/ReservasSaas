namespace ReservasSaaS.Domain.Ports.Outgoing;

/// <summary>
/// Secondary port for obtaining system time in a testable, deterministic way.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTimeOffset UtcNowOffset { get; }
}
