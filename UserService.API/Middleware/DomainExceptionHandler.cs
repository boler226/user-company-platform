using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UserService.Domain.Memberships;

namespace UsersService.API.Middleware;

public sealed class DomainExceptionHandler(
    IProblemDetailsService problems,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch
        {
            ResourceNotFoundException => StatusCodes.Status404NotFound,
            BusinessConflictException => StatusCodes.Status409Conflict,
            InputValidationException or FluentValidation.ValidationException => StatusCodes.Status400BadRequest,
            HttpRequestException => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };

        if (status >= 500)
        {
            logger.LogError(exception, "Request failed: {TraceId}", context.TraceIdentifier);
        }

        context.Response.StatusCode = status;
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = status switch
                {
                    400 => "Invalid request",
                    404 => "Resource not found",
                    409 => "Business conflict",
                    503 => "A dependent service is unavailable",
                    _ => "An unexpected error occurred"
                },
                Detail = exception is ResourceNotFoundException or BusinessConflictException or InputValidationException
                    ? exception.Message : null,
                Instance = context.Request.Path,
                Extensions = { ["traceId"] = context.TraceIdentifier }
            }
        });
        return true;
    }
}
