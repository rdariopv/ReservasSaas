using System;
using System.Collections.Generic;
using System.Text;

namespace ReservasSaaS.Application.UseCases.Auth
{

   

    public record RegisterUserResult(
        Guid Id,
        string FirebaseUid,
        string Email,
        string DisplayName);

}
