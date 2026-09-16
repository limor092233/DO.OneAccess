using Microsoft.AspNetCore.Mvc;
using DO.OneAccess.Application.Common.Exceptions;

namespace DO.OneAccess.Server.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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
        var statusCode = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ForbiddenException => StatusCodes.Status403Forbidden,
            ConflictException => StatusCodes.Status409Conflict,
            ValidationException => StatusCodes.Status400BadRequest,
            AuthenticationException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Request failed with status {StatusCode}: {Message}", statusCode, exception.Message);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        if (exception is ValidationException validationException)
        {
            var problemDetails = new ValidationProblemDetails(validationException.Errors.ToDictionary(k => k.Key, v => v.Value))
            {
                Status = statusCode,
                Title = "Validation Failed",
                Detail = validationException.Message,
                Instance = context.Request.Path
            };
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
        else
        {
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = GetTitleForStatusCode(statusCode),
                Detail = exception.Message,
                Instance = context.Request.Path
            };
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }

    private static string GetTitleForStatusCode(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        _ => "An error occurred while processing your request."
    };
}
