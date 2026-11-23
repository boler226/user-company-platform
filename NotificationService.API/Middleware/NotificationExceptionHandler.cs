using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using NotificationService.Domain.Exceptions;

namespace NotificationService.API.Middleware;

public sealed class NotificationExceptionHandler(ILogger<NotificationExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ValidationException or DomainException => StatusCodes.Status400BadRequest,
            ConflictException => StatusCodes.Status409Conflict,
            MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey } => StatusCodes.Status409Conflict,
            MongoBulkWriteException bulk when bulk.WriteErrors.Any(error => error.Category == ServerErrorCategory.DuplicateKey)
                => StatusCodes.Status409Conflict,
            MongoConnectionException or TimeoutException => StatusCodes.Status503ServiceUnavailable,
            MongoWriteException or MongoBulkWriteException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        logger.LogError(exception, "Request {Method} {Path} failed with {StatusCode}",
            context.Request.Method, context.Request.Path, status);
        var title = status switch
        {
            400 => "Invalid request",
            404 => "Resource not found",
            409 => "Resource conflict",
            503 => "Database unavailable",
            _ => "Unexpected error"
        };
        ProblemDetails problem;
        if (exception is ValidationException validation)
        {
            var errors = validation.Errors.GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).Distinct().ToArray());
            problem = new ValidationProblemDetails(errors);
        }
        else
        {
            problem = new ProblemDetails();
        }

        problem.Status = status;
        problem.Title = title;
        problem.Detail = exception is DomainException or NotFoundException or ConflictException
            ? exception.Message
            : title;
        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = status;
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        });
        return true;
    }
}
