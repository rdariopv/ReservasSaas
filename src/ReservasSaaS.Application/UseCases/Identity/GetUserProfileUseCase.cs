using Microsoft.Extensions.Logging;
using ReservasSaaS.Application.DTOs.Identity;
using ReservasSaaS.Application.Ports.Incoming;
using ReservasSaaS.Application.Security;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Exceptions;
using ReservasSaaS.Domain.Ports.Outgoing;

namespace ReservasSaaS.Application.UseCases.Identity;

public class GetUserProfileUseCase : IGetUserProfileUseCase
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserProfileUseCase> _logger;

    public GetUserProfileUseCase(
        ICurrentUserContext currentUserContext,
        IUserRepository userRepository,
        ILogger<GetUserProfileUseCase> logger)
    {
        _currentUserContext = currentUserContext;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<UserProfileDto> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUserContext.FirebaseUid))
        {
            throw new UnauthorizedException("User is not authenticated with a valid Firebase ID token.");
        }

        var firebaseUid = _currentUserContext.FirebaseUid;
        var user = await _userRepository.GetByFirebaseUidAsync(firebaseUid, cancellationToken);

        if (user == null)
        {
            // Auto-provision user entity on first verified Firebase authentication
            var email = _currentUserContext.Email ?? $"{firebaseUid}@firebase.auth";
            var displayName = email.Split('@')[0];

            _logger.LogInformation("Auto-provisioning new SaaS user profile for FirebaseUid '{FirebaseUid}'", firebaseUid);

            user = new User(
                id: Guid.NewGuid(),
                firebaseUid: firebaseUid,
                email: email,
                displayName: displayName,
                isSaaSAdministrator: _currentUserContext.IsSaaSAdministrator
            );

            await _userRepository.AddAsync(user, cancellationToken);
        }

        return new UserProfileDto(
            Id: user.Id,
            FirebaseUid: user.FirebaseUid,
            Email: user.Email,
            DisplayName: user.DisplayName,
            PhoneNumber: user.PhoneNumber,
            PhotoUrl: user.PhotoUrl,
            IsSaaSAdministrator: user.IsSaaSAdministrator,
            CreatedAtUtc: user.CreatedAtUtc
        );
    }
}
