using FirebaseAdmin.Auth;
using ReservasSaaS.Application.Ports.Outgoing;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReservasSaaS.Infrastructure.Authentication
{
    public class FirebaseIdentityService
    : IFirebaseIdentityService
    {
        public async Task<string> CreateUserAsync(
            string email,
            string password,
            string displayName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var args = new UserRecordArgs
            {
                Email = email,
                Password = password,
                DisplayName = displayName,
                EmailVerified = false,
                Disabled = false
            };

            var userRecord = await FirebaseAuth.DefaultInstance
                .CreateUserAsync(args)
                .WaitAsync(cancellationToken);

            return userRecord.Uid;
        }
    }
}
