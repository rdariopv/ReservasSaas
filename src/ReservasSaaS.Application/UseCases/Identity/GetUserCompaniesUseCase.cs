using Microsoft.Extensions.Logging;
using ReservasSaaS.Application.DTOs.Identity;
using ReservasSaaS.Application.Ports.Incoming;
using ReservasSaaS.Application.Security;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Domain.Security;

namespace ReservasSaaS.Application.UseCases.Identity;

public class GetUserCompaniesUseCase : IGetUserCompaniesUseCase
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUserRepository _userRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanyMembershipRepository _membershipRepository;
    private readonly ILogger<GetUserCompaniesUseCase> _logger;

    public GetUserCompaniesUseCase(
        ICurrentUserContext currentUserContext,
        IUserRepository userRepository,
        ICompanyRepository companyRepository,
        ICompanyMembershipRepository membershipRepository,
        ILogger<GetUserCompaniesUseCase> logger)
    {
        _currentUserContext = currentUserContext;
        _userRepository = userRepository;
        _companyRepository = companyRepository;
        _membershipRepository = membershipRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserCompanyAccessDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUserContext.FirebaseUid))
        {
            throw new UnauthorizedException("User is not authenticated with a valid Firebase ID token.");
        }

        var user = await _userRepository.GetByFirebaseUidAsync(_currentUserContext.FirebaseUid, cancellationToken);
        if (user == null)
        {
            return Array.Empty<UserCompanyAccessDto>();
        }

        var results = new List<UserCompanyAccessDto>();

        // If user is global SaaSAdministrator, they can manage and view all active companies
        if (user.IsSaaSAdministrator)
        {
            var allCompanies = await _companyRepository.GetAllAsync(cancellationToken);
            var adminPermissions = RolePermissionsMatrix.GetPermissionsForRole(CompanyRole.SaaSAdministrator);

            foreach (var comp in allCompanies.Where(c => c.Status != CompanyStatus.Archived))
            {
                results.Add(new UserCompanyAccessDto(
                    CompanyId: comp.Id,
                    CompanyName: comp.Name,
                    CompanySlug: comp.Slug,
                    CompanyStatus: comp.Status,
                    UserRole: CompanyRole.SaaSAdministrator,
                    MembershipStatus: MembershipStatus.Active,
                    Permissions: adminPermissions
                ));
            }

            return results;
        }

        // Standard tenant user: fetch memberships from database
        var memberships = await _membershipRepository.GetUserMembershipsAsync(user.Id, cancellationToken);

        foreach (var membership in memberships.Where(m => m.Status == MembershipStatus.Active))
        {
            var company = await _companyRepository.GetByIdAsync(membership.CompanyId, cancellationToken);
            if (company == null || company.Status == CompanyStatus.Archived)
            {
                continue;
            }

            var permissions = membership.GetEffectivePermissions();

            results.Add(new UserCompanyAccessDto(
                CompanyId: company.Id,
                CompanyName: company.Name,
                CompanySlug: company.Slug,
                CompanyStatus: company.Status,
                UserRole: membership.Role,
                MembershipStatus: membership.Status,
                Permissions: permissions
            ));
        }

        _logger.LogInformation("Resolved {Count} accessible companies for user '{UserId}'", results.Count, user.Id);

        return results;
    }
}
