using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace ReservasSaaS.Infrastructure.Logging;

public static class SerilogLoggingConfiguration
{
    public static ILogger CreateBootstrapLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateBootstrapLogger();
    }

    public static void ConfigureSerilog(LoggerConfiguration configuration, IConfiguration envConfig)
    {
        var minLevelStr = envConfig["Logging:LogLevel:Default"] ?? "Information";
        var parsedLevel = Enum.TryParse<LogEventLevel>(minLevelStr, true, out var level) 
            ? level 
            : LogEventLevel.Information;

        configuration
            .MinimumLevel.Is(parsedLevel)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "ReservasSaaS.Api")
            .Enrich.WithProperty("Environment", envConfig["ASPNETCORE_ENVIRONMENT"] ?? "Production")
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{Application}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}");
    }
}
