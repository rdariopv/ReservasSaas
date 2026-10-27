namespace ReservasSaaS.Application.DTOs.Identity;

public record CreateCompanyCommand(
    string Name,
    string Slug,
    string? TaxId = null,
    string TimeZone = "America/Bogota",
    string Currency = "COP");
