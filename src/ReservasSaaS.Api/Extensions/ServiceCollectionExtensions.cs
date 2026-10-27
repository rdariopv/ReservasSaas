using ReservasSaaS.Application;
using ReservasSaaS.Infrastructure;

namespace ReservasSaaS.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHexagonalArchitecture(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // 1. Core Application Layer (Use Cases, Inbound Ports)
        services.AddApplicationServices();

        // 2. Infrastructure Layer (Adapters, Outbound Ports, External Services)
        services.AddInfrastructureServices(configuration);

        // 3. API Context Adapters
        services.AddHttpContextAccessor();
        services.AddScoped<Application.Security.ICurrentUserContext, Security.CurrentUserContext>();
        services.AddScoped<Application.Security.ITenantContext, Security.TenantContext>();

        return services;
    }
}
