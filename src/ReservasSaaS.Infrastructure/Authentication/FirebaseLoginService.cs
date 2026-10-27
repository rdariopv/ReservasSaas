using Microsoft.Extensions.Configuration;
using ReservasSaaS.Application.Ports.Outgoing;
using ReservasSaaS.Application.UseCases.Auth;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ReservasSaaS.Infrastructure.Authentication
{
    public class FirebaseLoginService : IFirebaseLoginService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public FirebaseLoginService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;

            _apiKey = configuration["FirebaseAuthentication:WebApiKey"]
                ?? throw new InvalidOperationException(
                    "Falta configurar FirebaseAuthentication:WebApiKey.");
        }

        public async Task<LoginUserResult> LoginAsync(
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            var url =
                $"https://identitytoolkit.googleapis.com/v1/" +
                $"accounts:signInWithPassword?key={_apiKey}";

            using var response = await _httpClient.PostAsJsonAsync(
                url,
                new
                {
                    email,
                    password,
                    returnSecureToken = true
                },
                cancellationToken);

            using var content = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorCode = content.RootElement
                    .GetProperty("error")
                    .GetProperty("message")
                    .GetString();

                throw new UnauthorizedAccessException(
                    $"Firebase rechazó el inicio de sesión: {errorCode}");
            }

            var root = content.RootElement;

            return new LoginUserResult(
                IdToken: root.GetProperty("idToken").GetString()!,
                RefreshToken: root.GetProperty("refreshToken").GetString()!,
                ExpiresIn: root.GetProperty("expiresIn").GetString()!,
                LocalId: root.GetProperty("localId").GetString()!,
                Email: root.GetProperty("email").GetString()!);
        }
    }
}
