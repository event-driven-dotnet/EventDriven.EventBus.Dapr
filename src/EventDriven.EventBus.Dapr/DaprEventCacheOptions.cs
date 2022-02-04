using EventDriven.EventBus.Abstractions;

namespace EventDriven.EventBus.Dapr;

/// <summary>
/// Dapr event cache options.
/// </summary>
public class DaprEventCacheOptions : EventCacheOptions
{
    /// <summary>
    /// Dapr State Store options.
    /// </summary>
    public DaprStateStoreOptions DaprStateStoreOptions { get; set; } = new();
}