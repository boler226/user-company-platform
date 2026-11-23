namespace Platform.Aggregator.Models;

public sealed record MembershipDto(Guid Id, Guid UserId, Guid CompanyId,
    string CompanyNameSnapshot, string RequestedRole, string Status, long Version);
public sealed record CompanyDto(Guid Id, string Name, long Version);
public sealed record NotificationDto(string Id, string Status, int Priority,
    Dictionary<string, string> Payload, DateTime CreatedAt, long Version);
public sealed record PageDto<T>(IReadOnlyList<T> Items, long TotalCount, int Page, int PageSize);
public sealed record ServiceWarning(string Service, string Code, string Message);
public sealed record ServiceResult<T>(T? Data, ServiceWarning? Warning) where T : class;
public sealed record UserOverviewDto(Guid UserId, IReadOnlyList<MembershipDto>? Memberships,
    IReadOnlyList<CompanyDto>? Companies, PageDto<NotificationDto>? Notifications,
    IReadOnlyList<ServiceWarning> Warnings, int MembershipPage);
