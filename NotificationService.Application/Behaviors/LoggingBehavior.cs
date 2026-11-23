using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace NotificationService.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private static readonly ActivitySource Activities = new("NotificationService.Business");

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        using var activity = Activities.StartActivity(requestName);
        activity?.SetTag("business.operation", requestName);
        logger.LogInformation("Handling {RequestName}", requestName);
        try
        {
            var response = await next();
            logger.LogInformation("Completed {RequestName}", requestName);
            return response;
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            logger.LogWarning("Failed {RequestName} with {ErrorType}", requestName, exception.GetType().Name);
            throw;
        }
    }
}
