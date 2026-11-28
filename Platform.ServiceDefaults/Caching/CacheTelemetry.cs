using System.Diagnostics.Metrics;

namespace Platform.ServiceDefaults.Caching;

public sealed class CacheTelemetry(IMeterFactory factory)
{
    public const string MeterName = "Platform.Cache";

    private readonly Counter<long> hits = factory.Create(MeterName).CreateCounter<long>("cache.hits");
    private readonly Counter<long> misses = factory.Create(MeterName).CreateCounter<long>("cache.misses");
    private readonly Counter<long> invalidations = factory.Create(MeterName).CreateCounter<long>("cache.invalidations");

    public void RecordLookup(string cache, string level, bool hit)
    {
        (hit ? hits : misses).Add(1, new("cache.name", cache), new("cache.level", level));
    }

    public void RecordInvalidation(string cache)
    {
        invalidations.Add(1, new KeyValuePair<string, object?>("cache.name", cache));
    }
}
