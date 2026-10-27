using FirebaseAdmin.Auth;
using ReservasSaaS.Application.UseCases.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReservasSaaS.Infrastructure.Authentication
{
    public class FirebaseIdentityRegistrationService : IIdentityRegistrationService
    {
        public async Task<RegisteredIdentity> CreateUserAsync(
            string email,
            string password,
            string name,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await FirebaseAuth.DefaultInstance
                .CreateUserAsync(new UserRecordArgs
                {
                    Email = email.Trim(),
                    Password = password,
                    DisplayName = name.Trim(),
                    EmailVerified = false,
                    Disabled = false
                });

            return new RegisteredIdentity(
                user.Uid,
                user.Email ?? email.Trim(),
                user.DisplayName ?? name.Trim());
        }
    }
}
