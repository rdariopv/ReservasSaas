using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReservasSaaS.Infrastructure.Authentication
{
    public class FirebaseAdminInitializer
    {
        public static void Initialize(string projectId)
        {
            if (FirebaseApp.DefaultInstance is not null)
                return;

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.GetApplicationDefault(),
                ProjectId = projectId
            });
        }
    }
}
