using Microsoft.Extensions.Caching.Distributed;

namespace EventDriven.EventBus.EventCache.Redis;

/// <summary>
/// Redis event cache settings.
/// </summary>
public class RedisEventCacheSettings
{
    /// <summary>
    /// Connection string.
    /// </summary>
    public string ConnectionString { get; set; } = null!;

    /// <summary>
    /// Instance name.
    /// </summary>
    public string InstanceName { get; set; } = "eventcache-";

    /// <summary>
    /// Distributed cache entry options.
    /// </summary>
    public DistributedCacheEntryOptions DistributedCacheEntryOptions { get; set; } = new();
}