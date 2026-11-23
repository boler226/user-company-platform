using Platform.Aggregator.Models;

namespace Platform.Aggregator.Clients;

public sealed class UsersClient(HttpClient http, ILogger<UsersClient> logger)
    : ServiceClient(http, logger, "users-api")
{
    public Task<ServiceResult<IReadOnlyList<MembershipDto>>> GetMembershipsAsync(Guid userId, int page, CancellationToken ct) =>
        GetAsync<IReadOnlyList<MembershipDto>>($"/api/memberships?userId={userId}&page={page}", ct);
}
