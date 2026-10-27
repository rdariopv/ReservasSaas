using ReservasSaaS.Application.Ports.Outgoing;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReservasSaaS.Application.UseCases.Auth
{
    public class LoginUserUseCase : ILoginUserUseCase
    {
        private readonly IFirebaseLoginService _firebaseLoginService;

        public LoginUserUseCase(
            IFirebaseLoginService firebaseLoginService)
        {
            _firebaseLoginService = firebaseLoginService;
        }

        public Task<LoginUserResult> ExecuteAsync(
            LoginUserCommand command,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(command.Email))
                throw new ArgumentException("El correo es obligatorio.");

            if (string.IsNullOrWhiteSpace(command.Password))
                throw new ArgumentException("La contraseña es obligatoria.");

            return _firebaseLoginService.LoginAsync(
                command.Email.Trim(),
                command.Password,
                cancellationToken);
        }
    }
}
