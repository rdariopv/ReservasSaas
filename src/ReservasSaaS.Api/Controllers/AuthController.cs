using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservasSaaS.Application.UseCases.Auth;

namespace ReservasSaaS.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IRegisterUserUseCase _registerUserUseCase;
        private readonly ILoginUserUseCase _loginUserUseCase;

        public AuthController(
            IRegisterUserUseCase registerUserUseCase,
            ILoginUserUseCase loginUserUseCase)
        {
            _registerUserUseCase = registerUserUseCase;
            _loginUserUseCase = loginUserUseCase;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterUserCommand command,
            CancellationToken cancellationToken)
        {
            var user = await _registerUserUseCase.ExecuteAsync(
                command,
                cancellationToken);

            return Ok(user);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(
       [FromBody] LoginUserCommand command,
       CancellationToken cancellationToken)
        {
            var result = await _loginUserUseCase.ExecuteAsync(
                command,
                cancellationToken);

            return Ok(new
            {
                success = true,
                data = result
            });
        }
    }
}
