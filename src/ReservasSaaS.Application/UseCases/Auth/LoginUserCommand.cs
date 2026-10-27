using System;
using System.Collections.Generic;
using System.Text;

namespace ReservasSaaS.Application.UseCases.Auth
{
    public sealed record LoginUserCommand(
    string Email,
    string Password);
}
