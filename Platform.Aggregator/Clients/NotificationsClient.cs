using Platform.Aggregator.Models;

namespace Platform.Aggregator.Clients;

public sealed class NotificationsClient(HttpClient http, ILogger<NotificationsClient> logger)
    : ServiceClient(http, logger, "notifications-api")
{
    public Task<ServiceResult<PageDto<NotificationDto>>> GetAsync(Guid userId, int page, CancellationToken ct) =>
        GetAsync<PageDto<NotificationDto>>($"/api/notifications?userId={userId}&page={page}&pageSize=20", ct);
}
