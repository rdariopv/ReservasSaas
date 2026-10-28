using ReservasSaaS.Api.Extensions;
using ReservasSaaS.Api.Middleware;
using ReservasSaaS.Application.Ports.Incoming;
using ReservasSaaS.Infrastructure.Logging;
using Serilog;
using Scalar.AspNetCore;

Log.Logger = SerilogLoggingConfiguration.CreateBootstrapLogger();

try
{
    Log.Information("Starting ReservasSaaS.Api (Phase 1, .NET 10)...");

    var builder = WebApplication.CreateBuilder(args);

    // 1. Serilog Logging Setup
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        SerilogLoggingConfiguration.ConfigureSerilog(configuration, context.Configuration);
    });

    // 2. Configuration by Environment
    builder.Configuration
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();

    // 3. Dependency Injection (Hexagonal Architecture)
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();
    builder.Services.AddHexagonalArchitecture(builder.Configuration);

    // 4. CORS configuration for frontend / clients
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithExposedHeaders("X-Correlation-Id");
        });
    });

    var app = builder.Build();

    // 5. HTTP Pipeline Configuration
    // Global exception handler must be first to catch all pipeline errors
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    // Firebase Authentication & Server-Side Multi-Tenant Authorization
    app.UseMiddleware<FirebaseAuthenticationMiddleware>();
    app.UseMiddleware<TenantAuthorizationMiddleware>();

    app.UseCors("AllowAll");

    //if (app.Environment.IsDevelopment())
   // {
        app.MapOpenApi();
        app.MapScalarApiReference();
       // Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST","127.0.0.1:9099");
    //}

    app.UseHttpsRedirection();

    // 6. Controllers and Health Minimal Endpoint
    app.MapControllers();

    // Direct Minimal API health endpoint mapped to root /health as well
    app.MapGet("/", () => Results.Ok(new
    {
        service = "ReservasSaaS.Api",
        version = "1.0.0-phase1",
        runtime = ".NET 10",
        architecture = "Hexagonal / Clean Architecture",
        healthEndpoint = "/health",
        status = "Online"
    })).WithName("RootStatus");

    Log.Information("ReservasSaaS.Api configured successfully in {Environment} mode.", app.Environment.EnvironmentName);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ReservasSaaS.Api terminated unexpectedly during boot.");
}
finally
{
    Log.CloseAndFlush();
}
