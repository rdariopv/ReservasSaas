using ReservasSaaS.Application.Ports.Outgoing;
using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Ports.Outgoing;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReservasSaaS.Application.UseCases.Auth
{
    public class RegisterUserUseCase : IRegisterUserUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IFirebaseIdentityService _firebaseIdentityService;

        public RegisterUserUseCase(
        IUserRepository userRepository,
        IFirebaseIdentityService firebaseIdentityService)
        {
            _userRepository = userRepository;
            _firebaseIdentityService = firebaseIdentityService;
        }

        public async Task<RegisterUserResult> ExecuteAsync(
            RegisterUserCommand command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            if (string.IsNullOrWhiteSpace(command.Name))
                throw new ArgumentException("El nombre es obligatorio.");

            if (string.IsNullOrWhiteSpace(command.Email))
                throw new ArgumentException("El correo electrónico es obligatorio.");

            if (string.IsNullOrWhiteSpace(command.Password))
                throw new ArgumentException("La contraseña es obligatoria.");

            var email = command.Email.Trim().ToLowerInvariant();

            // 1. Crear la identidad en Firebase Authentication.
            var firebaseUid = await _firebaseIdentityService
                .CreateUserAsync(
                    email,
                    command.Password,
                    command.Name.Trim(),
                    cancellationToken);

            // 2. Crear el perfil de dominio.
            var user = new User(
                Guid.NewGuid(),
                firebaseUid,
                email,
                command.Name.Trim());

            // 3. Persistir el perfil en Firestore.
            await _userRepository.AddAsync(user, cancellationToken);

            // 4. Devolver el resultado sin la contraseña.
            return new RegisterUserResult(
                user.Id,
                user.FirebaseUid,
                user.Email,
                user.DisplayName);
        }
    }
}
