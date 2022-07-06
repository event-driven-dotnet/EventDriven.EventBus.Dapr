using EventDriven.EventBus.Abstractions;

namespace EventDriven.EventBus.Dapr.EventCache.Mongo;

/// <summary>
/// Dapr event cache options.
/// </summary>
public class DaprEventCacheOptions : EventCacheOptions
{
    /// <summary>
    /// Dapr event cache type.
    /// </summary>
    public DaprEventCacheType DaprEventCacheType { get; set; }
    
    /// <summary>
    /// Dapr State Store options.
    /// </summary>
    public DaprStateStoreOptions DaprStateStoreOptions { get; set; } = new();
}