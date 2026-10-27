using ReservasSaaS.Application.DTOs.Identity;

namespace ReservasSaaS.Application.Ports.Incoming;

public interface IGetUserProfileUseCase
{
    Task<UserProfileDto> ExecuteAsync(CancellationToken cancellationToken = default);
}
