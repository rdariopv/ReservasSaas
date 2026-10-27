using System;
using System.Collections.Generic;
using System.Text;

namespace ReservasSaaS.Application.UseCases.Auth
{

    public interface IIdentityRegistrationService
    {
        Task<RegisteredIdentity> CreateUserAsync(
            string email,
            string password,
            string name,
            CancellationToken cancellationToken);
    }

    public sealed record RegisteredIdentity(
        string Uid,
        string Email,
        string Name);
}
