using ReservasSaaS.Domain.Enums;

namespace ReservasSaaS.Application.DTOs.Identity;

public record CompanyDto(
    Guid Id,
    string Name,
    string Slug,
    string? TaxId,
    string TimeZone,
    string Currency,
    CompanyStatus Status,
    DateTime CreatedAtUtc);
