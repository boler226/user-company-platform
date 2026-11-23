namespace NotificationService.Application.Common;

public sealed class RecipientDto
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
}

public sealed class CompanySnapshotDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class RecentAttemptDto
{
    public string AttemptId { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime OccurredAt { get; set; }
}

public sealed class NotificationDto
{
    public string Id { get; set; } = "";
    public string TemplateId { get; set; } = "";
    public RecipientDto Recipient { get; set; } = new();
    public CompanySnapshotDto? Company { get; set; }
    public string Status { get; set; } = "";
    public int Priority { get; set; }
    public Dictionary<string, string> Payload { get; set; } = [];
    public List<RecentAttemptDto> RecentAttempts { get; set; } = [];
    public DateTime ScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long Version { get; set; }
}

public sealed class TemplateDto
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Channel { get; set; } = "";
    public string Body { get; set; } = "";
    public long Version { get; set; }
}

public interface INotificationQueries
{
    Task<NotificationDto?> GetByIdAsync(string id, CancellationToken ct);
    Task<PagedResult<NotificationDto>> GetListAsync(string? userId, string? status,
        string? search, int page, int pageSize, CancellationToken ct);
}

public interface ITemplateQueries
{
    Task<TemplateDto?> GetByIdAsync(string id, CancellationToken ct);
    Task<PagedResult<TemplateDto>> GetListAsync(int page, int pageSize, CancellationToken ct);
}
