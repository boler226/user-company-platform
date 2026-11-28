using Platform.Aggregator.Models;
using Platform.Grpc.Notifications.V1;

namespace Platform.Aggregator.Clients;

public sealed class NotificationsClient(NotificationReader.NotificationReaderClient client,
    ILogger<NotificationsClient> logger) : ServiceClient(logger, "notifications-api")
{
    public Task<ServiceResult<PageDto<NotificationDto>>> GetAsync(Guid userId, int page, CancellationToken ct) =>
        CallAsync(async (deadline, token) =>
        {
            var reply = await client.ListNotificationsAsync(
                new ListNotificationsRequest { UserId = userId.ToString(), Page = page, PageSize = 20 },
                deadline: deadline, cancellationToken: token);
            var items = reply.Items.Select(item => new NotificationDto(item.Id, item.Status, item.Priority,
                new Dictionary<string, string>(item.Payload), item.CreatedAt.ToDateTime(), item.Version)).ToArray();
            return new PageDto<NotificationDto>(items, reply.TotalCount, reply.Page, reply.PageSize);
        }, ct);
}
