using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace NotificationService.Application.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            return await next();
        }
        finally
        {
            timer.Stop();
            if (timer.ElapsedMilliseconds > 500)
                logger.LogWarning("Slow handler {RequestName}: {ElapsedMilliseconds} ms",
                    typeof(TRequest).Name, timer.ElapsedMilliseconds);
        }
    }
}
