using ReservasSaaS.Domain.Ports.Outgoing;

namespace ReservasSaaS.Infrastructure.Adapters;

/// <summary>
/// Infrastructure adapter implementing the domain outgoing port IDateTimeProvider.
/// </summary>
public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;
}
