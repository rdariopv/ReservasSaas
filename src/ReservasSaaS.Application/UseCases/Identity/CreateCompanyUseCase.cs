using Microsoft.Extensions.Logging;
using ReservasSaaS.Application.DTOs.Identity;
using ReservasSaaS.Application.Ports.Incoming;
using ReservasSaaS.Application.Security;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.Ports.Outgoing;

namespace ReservasSaaS.Application.UseCases.Identity;

public class CreateCompanyUseCase : ICreateCompanyUseCase
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUserRepository _userRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanyMembershipRepository _membershipRepository;
    private readonly ILogger<CreateCompanyUseCase> _logger;

    public CreateCompanyUseCase(
        ICurrentUserContext currentUserContext,
        IUserRepository userRepository,
        ICompanyRepository companyRepository,
        ICompanyMembershipRepository membershipRepository,
        ILogger<CreateCompanyUseCase> logger)
    {
        _currentUserContext = currentUserContext;
        _userRepository = userRepository;
        _companyRepository = companyRepository;
        _membershipRepository = membershipRepository;
        _logger = logger;
    }

    public async Task<CompanyDto> ExecuteAsync(CreateCompanyCommand command, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUserContext.FirebaseUid))
        {
            throw new UnauthorizedException("User must be authenticated to register a company.");
        }

        // Ensure user exists
        var user = await _userRepository.GetByFirebaseUidAsync(_currentUserContext.FirebaseUid, cancellationToken);
        if (user == null)
        {
            var email = _currentUserContext.Email ?? $"{_currentUserContext.FirebaseUid}@firebase.auth";
            user = new User(
                id: Guid.NewGuid(),
                firebaseUid: _currentUserContext.FirebaseUid,
                email: email,
                displayName: email.Split('@')[0]
            );
            await _userRepository.AddAsync(user, cancellationToken);
        }

        // Validate slug uniqueness
        var cleanSlug = command.Slug.Trim().ToLowerInvariant();
        var existing = await _companyRepository.GetBySlugAsync(cleanSlug, cancellationToken);
        if (existing != null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Slug"] = new[] { $"The slug '{cleanSlug}' is already taken by another company." }
            });
        }

        // Create Company aggregate root
        var companyId = Guid.NewGuid();
        var company = new Company(
            id: companyId,
            name: command.Name,
            slug: cleanSlug,
            taxId: command.TaxId,
            timeZone: command.TimeZone,
            currency: command.Currency
        );

        await _companyRepository.AddAsync(company, cancellationToken);

        // Assign caller as Owner
        var membershipId = Guid.NewGuid();
        var ownerMembership = new CompanyMembership(
            id: membershipId,
            companyId: companyId,
            userId: user.Id,
            role: CompanyRole.Owner,
            status: MembershipStatus.Active
        );

        await _membershipRepository.AddAsync(ownerMembership, cancellationToken);

        _logger.LogInformation("Company '{CompanyName}' ({CompanyId}) created by user '{UserId}' with Owner role",
            company.Name, company.Id, user.Id);

        return new CompanyDto(
            Id: company.Id,
            Name: company.Name,
            Slug: company.Slug,
            TaxId: company.TaxId,
            TimeZone: company.TimeZone ?? "America/Bogota",
            Currency: company.Currency ?? "COP",
            Status: company.Status,
            CreatedAtUtc: company.CreatedAtUtc
        );
    }
}
