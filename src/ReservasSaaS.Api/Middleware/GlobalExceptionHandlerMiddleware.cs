using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ReservasSaaS.Domain.Exceptions;

namespace ReservasSaaS.Api.Middleware;

/// <summary>
/// Centralized error handling middleware compliant with RFC 7807 ProblemDetails specification.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate _next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment environment)
    {
        this._next = _next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var (statusCode, title, errorCode, validationErrors) = MapException(exception);

        _logger.LogError(
            exception,
            "Unhandled exception occurred. TraceId: {TraceId}, ErrorCode: {ErrorCode}, Path: {Path}, Method: {Method}",
            traceId,
            errorCode,
            context.Request.Path,
            context.Request.Method);

        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{(int)statusCode}",
            Title = title,
            Status = (int)statusCode,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["timestampUtc"] = DateTime.UtcNow;

        if (validationErrors != null)
        {
            problemDetails.Extensions["errors"] = validationErrors;
        }

        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().FullName;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
    }

    private static (HttpStatusCode StatusCode, string Title, string ErrorCode, IReadOnlyDictionary<string, string[]>? ValidationErrors) 
        MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "Validation Error",
                validationEx.Code,
                validationEx.Errors),

            EntityNotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                "Resource Not Found",
                notFoundEx.Code,
                null),

            ConcurrencyException concurrencyEx => (
                HttpStatusCode.Conflict,
                "Concurrency Conflict",
                concurrencyEx.Code,
                null),

            PersistenceException persistenceEx => (
                HttpStatusCode.ServiceUnavailable,
                "Data Store Unavailable",
                persistenceEx.Code,
                null),
            UnauthorizedException unauthEx => (
               HttpStatusCode.Unauthorized,
               "Unauthorized",
               unauthEx.Code,
               null),

            ForbiddenException forbiddenEx => (
                HttpStatusCode.Forbidden,
                "Forbidden",
                forbiddenEx.Code,
                null),
            DomainException domainEx => (
                HttpStatusCode.UnprocessableEntity,
                "Business Rule Violation",
                domainEx.Code,
                null),

            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                "Invalid Argument",
                "INVALID_ARGUMENT",
                null),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "UNAUTHORIZED_ACCESS",
                null),

            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected internal error occurred",
                "INTERNAL_SERVER_ERROR",
                null)
        };
    }
}
