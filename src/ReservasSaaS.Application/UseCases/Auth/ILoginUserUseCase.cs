using System;
using System.Collections.Generic;
using System.Text;

namespace ReservasSaaS.Application.UseCases.Auth
{
    public interface ILoginUserUseCase
    {
        Task<LoginUserResult> ExecuteAsync(
        LoginUserCommand command,
        CancellationToken cancellationToken);
    }
}
