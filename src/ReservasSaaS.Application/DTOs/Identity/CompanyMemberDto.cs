using ReservasSaaS.Domain.Enums;

namespace ReservasSaaS.Application.DTOs.Identity;

public record CompanyMemberDto(
    Guid MembershipId,
    Guid UserId,
    string DisplayName,
    string Email,
    CompanyRole Role,
    MembershipStatus Status,
    DateTime JoinedAtUtc);
