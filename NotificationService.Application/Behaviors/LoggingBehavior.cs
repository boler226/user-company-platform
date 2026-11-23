using MediatR;
using Microsoft.Extensions.Logging;

namespace NotificationService.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}: {@Request}", requestName, request);

        try
        {
            var response = await next();
            logger.LogInformation("Handled {RequestName}: {@Response}", requestName, response);
            return response;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed {RequestName}", requestName);
            throw;
        }
    }
}
