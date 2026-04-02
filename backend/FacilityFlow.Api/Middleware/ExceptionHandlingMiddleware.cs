using System.Text.Json;
using FacilityFlow.Core.Exceptions;

namespace FacilityFlow.Api.Middleware;

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

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            NotFoundException e            => (StatusCodes.Status404NotFound, e.Message),
            ForbiddenException e           => (StatusCodes.Status403Forbidden, e.Message),
            InvalidTransitionException e   => (StatusCodes.Status422UnprocessableEntity, e.Message),
            InvalidOperationException e    => (StatusCodes.Status400BadRequest, e.Message),
            _                              => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode == 500)
            _logger.LogError(ex, "Unhandled exception");

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = JsonSerializer.Serialize(new { error = message });
        await context.Response.WriteAsync(response);
    }
}
