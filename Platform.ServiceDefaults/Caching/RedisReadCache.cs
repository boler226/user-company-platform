using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Platform.ServiceDefaults.Caching;

public sealed class RedisReadCache(IServiceProvider services, CacheTelemetry metrics, ILogger<RedisReadCache> logger)
{
    public const string Companies = "companies";
    public const string Memberships = "memberships";
    public const string Notifications = "notifications";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private IDistributedCache? Cache => services.GetService<IDistributedCache>();

    public async Task<string?> GetRevisionAsync(string scope, CancellationToken ct)
    {
        var cache = Cache;
        if (cache is null)
            return null;

        try
        {
            return await cache.GetStringAsync(RevisionKey(scope), ct) ?? "initial";
        }
        catch (Exception error) when (IsCacheFailure(error))
        {
            logger.LogWarning("Redis revision unavailable for {CacheName}: {ErrorType}", scope, error.GetType().Name);
            return null;
        }
    }

    public async Task<T?> GetAsync<T>(string scope, string key, CancellationToken ct) where T : class
    {
        var cache = Cache;
        if (cache is null)
            return null;

        try
        {
            var bytes = await cache.GetAsync(key, ct);
            var value = bytes is null ? null : JsonSerializer.Deserialize<T>(bytes, Json);
            metrics.RecordLookup(scope, "L2", value is not null);
            logger.LogDebug("Redis {CacheOutcome} for {CacheName}", value is null ? "miss" : "hit", scope);
            return value;
        }
        catch (Exception error) when (IsCacheFailure(error) || error is JsonException)
        {
            metrics.RecordLookup(scope, "L2", false);
            logger.LogWarning("Redis read failed for {CacheName}: {ErrorType}", scope, error.GetType().Name);
            return null;
        }
    }

    public async Task SetAsync<T>(string scope, string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        var cache = Cache;
        if (cache is null)
            return;

        try
        {
            await cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(value, Json),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
        }
        catch (Exception error) when (IsCacheFailure(error))
        {
            logger.LogWarning("Redis write failed for {CacheName}: {ErrorType}", scope, error.GetType().Name);
        }
    }

    public async Task InvalidateAsync(string scope, string reason)
    {
        var cache = Cache;
        if (cache is null)
            return;

        try
        {
            await cache.SetStringAsync(RevisionKey(scope), Guid.NewGuid().ToString("N"),
                new DistributedCacheEntryOptions(), CancellationToken.None);
            metrics.RecordInvalidation(scope);
            logger.LogInformation("Invalidated {CacheName} cache after {Reason}", scope, reason);
        }
        catch (Exception error) when (IsCacheFailure(error))
        {
            logger.LogWarning("Redis invalidation failed for {CacheName} after {Reason}: {ErrorType}",
                scope, reason, error.GetType().Name);
        }
    }

    private static string RevisionKey(string scope) => $"lab06:revision:{scope}";

    private static bool IsCacheFailure(Exception error) => error is RedisException or TimeoutException;
}
