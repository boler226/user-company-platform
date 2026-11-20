using CompanyService.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CompanyService.API.Middleware;

public sealed class CompanyExceptionHandler(IProblemDetailsService problems, IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch
        {
            NotFoundException => 404,
            ValidationException => 400,
            ConflictException or DomainException => 409,
            _ => 500
        };

        ProblemDetails details = exception is ValidationException validation
            ? new ValidationProblemDetails(validation.Errors.GroupBy(x => x.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(x => x.ErrorMessage).Distinct().ToArray()))
            : new ProblemDetails();

        details.Status = status;
        details.Title = status switch
        {
            400 => "Validation failed",
            404 => "Resource not found",
            409 => "Operation conflicts with current data",
            _ => "Unexpected server error"
        };
        details.Detail = environment.IsDevelopment() ? exception.Message : null;
        details.Instance = context.Request.Path;
        details.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = status;
        // ExceptionHandlerMiddleware logs the exception once; do not log it again here.
        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = details });
        return true;
    }
}
