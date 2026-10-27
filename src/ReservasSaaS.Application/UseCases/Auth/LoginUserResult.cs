using System;
using System.Collections.Generic;
using System.Text;

namespace ReservasSaaS.Application.UseCases.Auth
{
    public sealed record LoginUserResult(
    string IdToken,
    string RefreshToken,
    string ExpiresIn,
    string LocalId,
    string Email);
}
