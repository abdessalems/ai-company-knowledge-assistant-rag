namespace AIKnowledgeAssistant.API.Middleware;

using System.Net;
using System.Text.Json;
using AIKnowledgeAssistant.Domain.Exceptions;
using FluentValidation;

/// <summary>
/// Global exception handling middleware.
/// 
/// PURPOSE:
/// Catches all unhandled exceptions
/// Converts them to appropriate HTTP responses
/// Provides consistent error format to clients
/// 
/// MIDDLEWARE PIPELINE:
/// Request → Logging → Authentication → Endpoints
///    ↑                                       ↓
///    └───────── Exception Handling ─────────┘
/// 
/// ALL exceptions flow through this middleware
/// Even if not caught in controllers
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invoke the middleware.
    /// 
    /// PROCESS:
    /// 1. Try to execute next middleware
    /// 2. If exception, catch it
    /// 3. Log the error
    /// 4. Convert to error response
    /// 5. Send to client
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the exception
            _logger.LogError(ex, "An unhandled exception occurred");

            // Convert exception to error response
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Convert different exception types to HTTP responses.
    /// </summary>
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // ====================================
        // DETERMINE STATUS CODE AND MESSAGE
        // ====================================

        HttpStatusCode code = HttpStatusCode.InternalServerError;
        string message = "An internal server error occurred";
        object? details = null;

        switch (exception)
        {
            // ====================================
            // DOMAIN EXCEPTIONS
            // ====================================
            
            case UnauthorizedAccessException ex:
                code = HttpStatusCode.Forbidden;
                message = ex.Message;
                break;

            case EntityNotFoundException ex:
                code = HttpStatusCode.NotFound;
                message = ex.Message;
                break;

            case InvalidBusinessRuleException ex:
                code = HttpStatusCode.BadRequest;
                message = ex.Message;
                break;

            case AuthenticationFailedException ex:
                code = HttpStatusCode.Unauthorized;
                message = ex.Message;
                break;

            // ====================================
            // FLUENT VALIDATION ERRORS
            // ====================================
            
            case ValidationException ex:
                code = HttpStatusCode.BadRequest;
                message = "Validation failed";
                details = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
                break;

            // ====================================
            // ARGUMENT EXCEPTIONS
            // ====================================
            
            // ArgumentNullException inherits from ArgumentException,
            // so this single case handles both.
            case ArgumentException ex:
                code = HttpStatusCode.BadRequest;
                message = ex.Message;
                break;

            // ====================================
            // UNKNOWN ERRORS
            // ====================================
            
            default:
                code = HttpStatusCode.InternalServerError;
                message = "An unexpected error occurred";
                // Don't expose internal error details to client
                break;
        }

        // ====================================
        // BUILD ERROR RESPONSE
        // ====================================

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        var response = new
        {
            error = new
            {
                message,
                details,
                code = (int)code,
                timestamp = DateTime.UtcNow
            }
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// Extension method to register error handling middleware.
/// </summary>
public static class ErrorHandlingMiddlewareExtensions
{
    /// <summary>
    /// Add error handling middleware to pipeline.
    /// 
    /// IMPORTANT: Must be called FIRST in pipeline
    /// Otherwise exceptions in other middleware won't be caught
    /// 
    /// EXAMPLE:
    /// var app = builder.Build();
    /// app.UseErrorHandling(); // First!
    /// app.UseAuthentication();
    /// app.MapControllers();
    /// </summary>
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
