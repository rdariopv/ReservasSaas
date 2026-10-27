using ReservasSaaS.Application.UseCases.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReservasSaaS.Application.Ports.Outgoing
{
    public interface IFirebaseLoginService
    {
        Task<LoginUserResult> LoginAsync(
       string email,
       string password,
       CancellationToken cancellationToken);
    }
}
