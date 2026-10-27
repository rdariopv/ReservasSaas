using Microsoft.Extensions.Logging;
using ReservasSaaS.Application.DTOs.Identity;
using ReservasSaaS.Application.Ports.Incoming;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Domain.Security;

namespace ReservasSaaS.Application.UseCases.Identity;

public class GetCompanyMembersUseCase : IGetCompanyMembersUseCase
{
    private readonly IAuthorizeCompanyAccessUseCase _authorizer;
    private readonly ICompanyMembershipRepository _membershipRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetCompanyMembersUseCase> _logger;

    public GetCompanyMembersUseCase(
        IAuthorizeCompanyAccessUseCase authorizer,
        ICompanyMembershipRepository membershipRepository,
        IUserRepository userRepository,
        ILogger<GetCompanyMembersUseCase> logger)
    {
        _authorizer = authorizer;
        _membershipRepository = membershipRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CompanyMemberDto>> ExecuteAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        // 1. Authorize on server side (must have MembersRead permission or be Owner/Admin)
        await _authorizer.AuthorizeAsync(companyId, Permissions.MembersRead, cancellationToken);

        // 2. Fetch all members in this company
        var memberships = await _membershipRepository.GetCompanyMembersAsync(companyId, cancellationToken);
        var dtos = new List<CompanyMemberDto>();

        foreach (var membership in memberships)
        {
            var memberUser = await _userRepository.GetByIdAsync(membership.UserId, cancellationToken);
            if (memberUser == null) continue;

            dtos.Add(new CompanyMemberDto(
                MembershipId: membership.Id,
                UserId: memberUser.Id,
                DisplayName: memberUser.DisplayName,
                Email: memberUser.Email,
                Role: membership.Role,
                Status: membership.Status,
                JoinedAtUtc: membership.CreatedAtUtc
            ));
        }

        return dtos;
    }
}
