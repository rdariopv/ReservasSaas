using System;
using System.Collections.Generic;
using System.Text;

namespace ReservasSaaS.Application.Ports.Outgoing
{
    public interface IFirebaseIdentityService
    {
        Task<string> CreateUserAsync(
     string email,
     string password,
     string displayName,
     CancellationToken cancellationToken);
    }
}
