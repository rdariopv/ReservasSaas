using ReservasSaaS.Domain.Enums;

namespace ReservasSaaS.Application.DTOs.Identity;

public record UserCompanyAccessDto(
    Guid CompanyId,
    string CompanyName,
    string CompanySlug,
    CompanyStatus CompanyStatus,
    CompanyRole UserRole,
    MembershipStatus MembershipStatus,
    IReadOnlyCollection<string> Permissions);
