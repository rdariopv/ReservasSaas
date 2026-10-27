using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReservasSaaS.Application.Ports.Outgoing;
using ReservasSaaS.Domain.Ports.Outgoing;
using ReservasSaaS.Infrastructure.Adapters;
using ReservasSaaS.Infrastructure.Authentication;
using ReservasSaaS.Infrastructure.Persistence.Firestore;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Configuration;
using ReservasSaaS.Infrastructure.Persistence.Firestore.Interfaces;

namespace ReservasSaaS.Infrastructure;

public static class DependencyInjection
{

    //private static void InitializeFirebaseAdmin(
    //IConfiguration configuration)
    //{
    //    if (FirebaseApp.DefaultInstance is not null)
    //        return;

    //    var projectId = configuration[
    //        "FirebaseAuthentication:ProjectId"];

    //    if (string.IsNullOrWhiteSpace(projectId))
    //    {
    //        throw new InvalidOperationException(
    //            "FirebaseAuthentication:ProjectId no está configurado.");
    //    }

    //    FirebaseApp.Create(new AppOptions
    //    {
    //        Credential = GoogleCredential.GetApplicationDefault(),
    //        ProjectId = projectId
    //    });
    //}
    private static void InitializeFirebaseAdmin(
    IConfiguration configuration)
    {
        if (FirebaseApp.DefaultInstance is not null)
            return;

        var projectId =
            configuration["FirebaseAuthentication:ProjectId"]
            ?? "demo-reservas-saas";

        var credentialsPath =  Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

        if (string.IsNullOrWhiteSpace(credentialsPath))
        {
            throw new InvalidOperationException(
                "No se encontró GOOGLE_APPLICATION_CREDENTIALS en el proceso de la API.");
        }

       

        FirebaseApp.Create(new AppOptions
        {
            ProjectId = projectId,
            Credential = GoogleCredential.GetApplicationDefault()
            // Credential = GoogleCredential.FromAccessToken("emulator-access-token")
        });
    }
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {

        InitializeFirebaseAdmin(configuration);


        // 1. Clock and OS diagnostics adapters
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<ISystemHealthCheckPort, SystemHealthCheckAdapter>();


        services.AddScoped< IFirebaseIdentityService, FirebaseIdentityService>();
        services.AddHttpClient<IFirebaseLoginService, FirebaseLoginService>();

        // 2. Firebase Cloud Firestore persistence registration
        services.AddFirestorePersistence(configuration);

        // 3. Identity and Multi-tenancy persistence repositories
        services.AddScoped<ICompanyRepository, Persistence.Firestore.Repositories.FirestoreCompanyRepository>();
        services.AddScoped<IUserRepository, Persistence.Firestore.Repositories.FirestoreUserRepository>();
        services.AddScoped<ICompanyMembershipRepository, Persistence.Firestore.Repositories.FirestoreCompanyMembershipRepository>();

        // 4. Services & Professionals repositories
        services.AddScoped<IServiceRepository, Persistence.Firestore.Repositories.FirestoreServiceRepository>();
        services.AddScoped<IProfessionalRepository, Persistence.Firestore.Repositories.FirestoreProfessionalRepository>();
        services.AddScoped<IBookingRepository, Persistence.Firestore.Repositories.FirestoreBookingRepository>();
        services.AddScoped<IExistingAppointmentsQueryPort, Adapters.FirestoreAppointmentsQueryAdapter>();

        // 5. Firebase Authentication Token Validation
        services.Configure<Authentication.FirebaseAuthenticationOptions>(
            configuration.GetSection(Authentication.FirebaseAuthenticationOptions.SectionName));
        services.AddSingleton<Authentication.IFirebaseTokenValidator, Authentication.FirebaseTokenValidator>();

        return services;
    }

    public static IServiceCollection AddFirestorePersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration section (Firestore)
        services.Configure<FirestoreOptions>(
            configuration.GetSection(FirestoreOptions.SectionName));

        // Register Firestore provider singleton
        services.AddSingleton<IFirestoreProvider, FirestoreProvider>();

        return services;
    }
}
