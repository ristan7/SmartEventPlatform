using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartEventPlatform.DirectoryService.Resilience;

namespace SmartEventPlatform.DirectoryService.ErrorHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found.", exception.Message),
            CircuitBreakerOpenException => (StatusCodes.Status503ServiceUnavailable, "Downstream circuit breaker is open.", exception.Message),
            TaskCanceledException => (StatusCodes.Status504GatewayTimeout, "Downstream request timed out.", "The downstream service did not respond in time."),
            HttpRequestException => (StatusCodes.Status503ServiceUnavailable, "Downstream service is unavailable.", exception.Message),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Business validation error.", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected server error.", "An unexpected error occurred.")
        };

        _logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        }, cancellationToken);

        return true;
    }
}