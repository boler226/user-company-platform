using System.Diagnostics;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Services;

public sealed class UserOverviewService(UsersClient users, CompaniesClient companies,
    NotificationsClient notifications, ILogger<UserOverviewService> logger)
{
    private static readonly ActivitySource Activities = new("Platform.Aggregator");

    public async Task<UserOverviewDto> GetAsync(Guid userId, int page, CancellationToken ct)
    {
        using var activity = Activities.StartActivity("users.overview");
        activity?.SetTag("user.id", userId);
        var membershipsTask = users.GetMembershipsAsync(userId, page, ct);
        var notificationsTask = notifications.GetAsync(userId, page, ct);
        await Task.WhenAll(membershipsTask, notificationsTask);
        var memberships = await membershipsTask;
        var messages = await notificationsTask;
        var companyIds = memberships.Data?.Select(item => item.CompanyId).Distinct().ToArray() ?? [];
        var companyResults = await Task.WhenAll(companyIds.Select(id => companies.GetAsync(id, ct)));

        var warnings = new List<ServiceWarning>();
        if (memberships.Warning is not null)
            warnings.Add(memberships.Warning);
        if (messages.Warning is not null)
            warnings.Add(messages.Warning);
        warnings.AddRange(companyResults.Where(result => result.Warning is not null).Select(result => result.Warning!));
        var companyData = memberships.Data is null
            ? null
            : companyResults.Where(result => result.Data is not null).Select(result => result.Data!).ToArray();

        activity?.SetTag("overview.warning_count", warnings.Count);
        logger.LogInformation("Built overview for {UserId} with {MembershipCount} memberships and {WarningCount} warnings",
            userId, memberships.Data?.Count, warnings.Count);
        return new UserOverviewDto(userId, memberships.Data, companyData, messages.Data,
            warnings.Distinct().ToArray(), page);
    }
}
