using EventDriven.EventBus.Abstractions;

namespace EventDriven.EventBus.EventCache.Mongo;

/// <summary>
/// Mongo event cache options.
/// </summary>
public class MongoEventCacheOptions : EventCacheOptions
{
    /// <summary>
    /// Application name.
    /// </summary>
    public string AppName { get; set; } = null!;
}