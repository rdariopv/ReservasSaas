using ReservasSaaS.Domain.Common;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;

namespace ReservasSaaS.Domain.Entities;

/// <summary>
/// Tenant entity representing a company, clinic, academy, or sports center in the SaaS platform.
/// </summary>
public class Company : BaseEntity<Guid>, IAggregateRoot
{
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? TaxId { get; private set; }
    public string? TimeZone { get; private set; }
    public string? Currency { get; private set; }
    public CompanyStatus Status { get; private set; }
    public ProfessionalAssignmentStrategy AssignmentStrategy { get; private set; }
    public int DefaultSlotIntervalMinutes { get; private set; }

    // Required for deserialization/mapping
    private Company()
    {
        Name = string.Empty;
        Slug = string.Empty;
        AssignmentStrategy = ProfessionalAssignmentStrategy.ClientSelectsProfessional;
        DefaultSlotIntervalMinutes = 30;
    }

    public Company(
        Guid id, 
        string name, 
        string slug, 
        string? taxId = null, 
        string timeZone = "America/Bogota", 
        string currency = "COP",
        ProfessionalAssignmentStrategy assignmentStrategy = ProfessionalAssignmentStrategy.ClientSelectsProfessional,
        int defaultSlotIntervalMinutes = 30)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Company name cannot be empty.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new ValidationException("Company slug cannot be empty.");

        if (defaultSlotIntervalMinutes <= 0)
            throw new ValidationException("Slot interval minutes must be positive.");

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        TaxId = taxId?.Trim();
        TimeZone = timeZone;
        Currency = currency;
        Status = CompanyStatus.Active;
        AssignmentStrategy = assignmentStrategy;
        DefaultSlotIntervalMinutes = defaultSlotIntervalMinutes;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string? taxId, string timeZone, string currency)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Company name cannot be empty.");

        Name = name.Trim();
        TaxId = taxId?.Trim();
        TimeZone = timeZone;
        Currency = currency;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetBookingSettings(ProfessionalAssignmentStrategy strategy, int slotIntervalMinutes)
    {
        if (slotIntervalMinutes <= 0)
            throw new ValidationException("Slot interval minutes must be positive.");

        AssignmentStrategy = strategy;
        DefaultSlotIntervalMinutes = slotIntervalMinutes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ChangeStatus(CompanyStatus newStatus)
    {
        Status = newStatus;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
