using System;
using System.Collections.Generic;
using System.Text;

namespace ReservasSaaS.Application.UseCases.Auth
{
    public interface IRegisterUserUseCase
    {
        Task<RegisterUserResult> ExecuteAsync(
      RegisterUserCommand command,
      CancellationToken cancellationToken);
    }
}
