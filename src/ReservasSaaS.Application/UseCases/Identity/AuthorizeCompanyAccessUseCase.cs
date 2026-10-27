using Microsoft.Extensions.Logging;
using ReservasSaaS.Application.Ports.Incoming;
using ReservasSaaS.Application.Security;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Domain.Security;

namespace ReservasSaaS.Application.UseCases.Identity;

/// <summary>
/// Server-side multi-tenant authorization engine.
/// Validates user identity, company existence, active membership, and granular permissions.
/// NEVER trusts client-supplied company identifiers blindly.
/// </summary>
public class AuthorizeCompanyAccessUseCase : IAuthorizeCompanyAccessUseCase
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUserRepository _userRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanyMembershipRepository _membershipRepository;
    private readonly ILogger<AuthorizeCompanyAccessUseCase> _logger;

    public AuthorizeCompanyAccessUseCase(
        ICurrentUserContext currentUserContext,
        IUserRepository userRepository,
        ICompanyRepository companyRepository,
        ICompanyMembershipRepository membershipRepository,
        ILogger<AuthorizeCompanyAccessUseCase> logger)
    {
        _currentUserContext = currentUserContext;
        _userRepository = userRepository;
        _companyRepository = companyRepository;
        _membershipRepository = membershipRepository;
        _logger = logger;
    }

    public async Task<CompanyAuthorizationResult> AuthorizeAsync(
        Guid companyId, 
        string? requiredPermission = null, 
        CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            throw new ValidationException("Target company identifier cannot be empty.");
        }

        if (!_currentUserContext.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUserContext.FirebaseUid))
        {
            _logger.LogWarning("Access denied to company '{CompanyId}': request is unauthenticated", companyId);
            throw new UnauthorizedException("User is not authenticated with a valid Firebase ID token.");
        }

        // 1. Verify target company exists
        var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);
        if (company == null)
        {
            _logger.LogWarning("Target company '{CompanyId}' not found", companyId);
            throw new EntityNotFoundException("Company", companyId);
        }

        if (company.Status == CompanyStatus.Suspended)
        {
            _logger.LogWarning("Access denied to company '{CompanyId}': company is suspended", companyId);
            throw new ForbiddenException($"Company '{company.Name}' is currently suspended. Contact support.");
        }

        if (company.Status == CompanyStatus.Archived)
        {
            throw new EntityNotFoundException("Company", companyId);
        }

        // 2. Resolve user in local database
        var user = await _userRepository.GetByFirebaseUidAsync(_currentUserContext.FirebaseUid, cancellationToken);
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("User with FirebaseUid '{FirebaseUid}' does not have an active database profile", _currentUserContext.FirebaseUid);
            throw new ForbiddenException("User profile is not active or does not exist.");
        }

        // 3. Global SaaSAdministrator bypass (has cross-tenant audit/management access)
        if (user.IsSaaSAdministrator)
        {
            _logger.LogInformation("Granted cross-tenant access to company '{CompanyId}' for SaaS Administrator '{UserId}'",
                companyId, user.Id);

            var saasAdminPermissions = RolePermissionsMatrix.GetPermissionsForRole(CompanyRole.SaaSAdministrator);
            return new CompanyAuthorizationResult(
                IsAuthorized: true,
                Company: company,
                Role: CompanyRole.SaaSAdministrator,
                EffectivePermissions: saasAdminPermissions
            );
        }

        // 4. Server-Side Membership Verification (CRITICAL ISOLATION ENFORCEMENT)
        var membership = await _membershipRepository.GetMembershipAsync(companyId, user.Id, cancellationToken);
        if (membership == null)
        {
            _logger.LogWarning(
                "CROSS-TENANT ACCESS VIOLATION: User '{UserId}' ({Email}) attempted to access company '{CompanyId}' without valid membership",
                user.Id, user.Email, companyId);

            throw new ForbiddenException(companyId.ToString());
        }

        if (membership.Status != MembershipStatus.Active)
        {
            _logger.LogWarning(
                "Access denied for user '{UserId}' in company '{CompanyId}': membership status is '{Status}'",
                user.Id, companyId, membership.Status);

            throw new ForbiddenException($"User membership in company '{company.Name}' is {membership.Status}. Access denied.");
        }

        // 5. Granular Permission Verification
        var effectivePermissions = membership.GetEffectivePermissions();
        if (!string.IsNullOrWhiteSpace(requiredPermission) && !effectivePermissions.Contains(requiredPermission))
        {
            _logger.LogWarning(
                "PERMISSION DENIED: User '{UserId}' with role '{Role}' in company '{CompanyId}' lacks required permission '{Permission}'",
                user.Id, membership.Role, companyId, requiredPermission);

            throw new ForbiddenException(companyId.ToString(), requiredPermission);
        }

        _logger.LogDebug("Authorized user '{UserId}' (Role: {Role}) for company '{CompanyId}'", user.Id, membership.Role, companyId);

        return new CompanyAuthorizationResult(
            IsAuthorized: true,
            Company: company,
            Role: membership.Role,
            EffectivePermissions: effectivePermissions
        );
    }
}
