using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Platform.ServiceDefaults.Caching;

namespace CompanyService.Application.Caching;

public sealed class CompanyReadCache(IMemoryCache memory, RedisReadCache redis, CacheTelemetry metrics,
    ILogger<CompanyReadCache> logger)
{
    public async Task<T> GetAsync<T>(string key, TimeSpan redisTtl, Func<Task<T>> load, CancellationToken ct)
        where T : class
    {
        var revision = await redis.GetRevisionAsync(RedisReadCache.Companies, ct);
        if (revision is null)
            return await load();

        var versionedKey = $"lab06:companies:{revision}:{key}";
        var hit = memory.TryGetValue<T>(versionedKey, out var value) && value is not null;
        metrics.RecordLookup(RedisReadCache.Companies, "L1", hit);
        logger.LogDebug("Memory cache {CacheOutcome} for companies", hit ? "hit" : "miss");
        if (hit)
            return value!;

        value = await redis.GetAsync<T>(RedisReadCache.Companies, versionedKey, ct);
        if (value is null)
        {
            value = await load();
            await redis.SetAsync(RedisReadCache.Companies, versionedKey, value, redisTtl, ct);
        }

        memory.Set(versionedKey, value, new MemoryCacheEntryOptions
        {
            Size = 1,
            Priority = CacheItemPriority.Normal,
            SlidingExpiration = TimeSpan.FromSeconds(5),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(15)
        });
        return value;
    }

    public Task InvalidateAsync(string reason) => redis.InvalidateAsync(RedisReadCache.Companies, reason);
}
