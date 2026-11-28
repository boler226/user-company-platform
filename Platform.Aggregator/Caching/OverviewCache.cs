using Platform.Aggregator.Models;
using Platform.ServiceDefaults.Caching;

namespace Platform.Aggregator.Caching;

public sealed class OverviewCache(RedisReadCache redis)
{
    private const string CacheName = "user-overview";

    public async Task<string?> GetKeyAsync(Guid userId, int page, CancellationToken ct)
    {
        var revisions = await Task.WhenAll(
            redis.GetRevisionAsync(RedisReadCache.Memberships, ct),
            redis.GetRevisionAsync(RedisReadCache.Companies, ct),
            redis.GetRevisionAsync(RedisReadCache.Notifications, ct));
        if (revisions.Any(revision => revision is null))
            return null;

        return $"lab06:overview:{userId:D}:{page}:{string.Join(':', revisions)}";
    }

    public Task<UserOverviewDto?> GetAsync(string key, CancellationToken ct) =>
        redis.GetAsync<UserOverviewDto>(CacheName, key, ct);

    public Task SetAsync(string key, UserOverviewDto overview, CancellationToken ct) =>
        redis.SetAsync(CacheName, key, overview, TimeSpan.FromSeconds(30), ct);
}
