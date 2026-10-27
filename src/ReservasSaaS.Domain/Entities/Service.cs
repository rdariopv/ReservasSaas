using ReservasSaaS.Domain.Common;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.ValueObjects;

namespace ReservasSaaS.Domain.Entities;

/// <summary>
/// Service aggregate root representing a bookable offer (haircut, medical consultation, padel court, etc.)
/// with duration, pricing, buffer margins, and deposit policy.
/// </summary>
public class Service : BaseEntity<Guid>, IAggregateRoot
{
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int DurationMinutes { get; private set; }
    public int BufferBeforeMinutes { get; private set; }
    public int BufferAfterMinutes { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; }
    public ServiceStatus Status { get; private set; }
    public DepositPolicy DepositPolicy { get; private set; }

    public int TotalOccupiedMinutes => DurationMinutes + BufferBeforeMinutes + BufferAfterMinutes;

    private Service()
    {
        Name = string.Empty;
        Description = string.Empty;
        Currency = "COP";
        DepositPolicy = DepositPolicy.None;
    }

    public Service(
        Guid id,
        Guid companyId,
        string name,
        string description,
        int durationMinutes,
        decimal price,
        string currency = "COP",
        int bufferBeforeMinutes = 0,
        int bufferAfterMinutes = 0,
        DepositPolicy? depositPolicy = null,
        ServiceStatus status = ServiceStatus.Active)
        : base(id)
    {
        if (companyId == Guid.Empty)
            throw new ValidationException("CompanyId cannot be empty.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Service name cannot be empty.");

        if (durationMinutes <= 0)
            throw new ValidationException("Service duration must be greater than zero minutes.");

        if (price < 0)
            throw new ValidationException("Service price cannot be negative.");

        if (bufferBeforeMinutes < 0 || bufferAfterMinutes < 0)
            throw new ValidationException("Buffer times cannot be negative.");

        CompanyId = companyId;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        DurationMinutes = durationMinutes;
        Price = price;
        Currency = string.IsNullOrWhiteSpace(currency) ? "COP" : currency.Trim().ToUpperInvariant();
        BufferBeforeMinutes = bufferBeforeMinutes;
        BufferAfterMinutes = bufferAfterMinutes;
        DepositPolicy = depositPolicy ?? DepositPolicy.None;
        Status = status;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(
        string name,
        string description,
        int durationMinutes,
        decimal price,
        string currency,
        int bufferBeforeMinutes,
        int bufferAfterMinutes,
        DepositPolicy depositPolicy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Service name cannot be empty.");

        if (durationMinutes <= 0)
            throw new ValidationException("Service duration must be greater than zero minutes.");

        if (price < 0)
            throw new ValidationException("Service price cannot be negative.");

        if (bufferBeforeMinutes < 0 || bufferAfterMinutes < 0)
            throw new ValidationException("Buffer times cannot be negative.");

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        DurationMinutes = durationMinutes;
        Price = price;
        Currency = string.IsNullOrWhiteSpace(currency) ? "COP" : currency.Trim().ToUpperInvariant();
        BufferBeforeMinutes = bufferBeforeMinutes;
        BufferAfterMinutes = bufferAfterMinutes;
        DepositPolicy = depositPolicy ?? DepositPolicy.None;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ChangeStatus(ServiceStatus newStatus)
    {
        Status = newStatus;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
