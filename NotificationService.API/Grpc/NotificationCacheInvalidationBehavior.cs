using MediatR;
using NotificationService.Application.Notifications;
using Platform.ServiceDefaults.Caching;

namespace NotificationService.API.Grpc;

public sealed class NotificationCacheInvalidationBehavior<TRequest, TResponse>(RedisReadCache cache)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var result = await next();
        if (request is CreateNotificationCommand or UpdateNotificationCommand or DeleteNotificationCommand)
            await cache.InvalidateAsync(RedisReadCache.Notifications, typeof(TRequest).Name);

        return result;
    }
}
