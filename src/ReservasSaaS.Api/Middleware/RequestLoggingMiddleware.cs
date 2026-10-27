using System.Diagnostics;

namespace ReservasSaaS.Api.Middleware;

/// <summary>
/// Middleware to attach Correlation ID and log HTTP request/response metrics with Serilog context.
/// </summary>
public class RequestLoggingMiddleware
{
    public const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract or generate Correlation ID
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) || string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId.ToString(),
            ["TraceId"] = Activity.Current?.Id ?? context.TraceIdentifier,
            ["RequestPath"] = context.Request.Path.Value ?? string.Empty,
            ["RequestMethod"] = context.Request.Method
        }))
        {
            var sw = Stopwatch.StartNew();

            _logger.LogInformation(
                "Incoming HTTP {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await _next(context);

            sw.Stop();

            _logger.LogInformation(
                "Completed HTTP {Method} {Path} -> {StatusCode} in {ElapsedMs:0.00} ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.Elapsed.TotalMilliseconds);
        }
    }
}
