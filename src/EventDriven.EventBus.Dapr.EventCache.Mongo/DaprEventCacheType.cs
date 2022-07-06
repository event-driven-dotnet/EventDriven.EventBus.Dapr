namespace EventDriven.EventBus.Dapr.EventCache.Mongo;

/// <summary>
/// Dapr event cache type.
/// </summary>
public enum DaprEventCacheType
{
    /// <summary>
    /// NonQueryable Dapr state store.
    /// Does not support event cache cleanup.
    /// </summary>
    NonQueryable,

    /// <summary>
    /// Queryable Dapr state store.
    /// Supports event cache cleanup.
    /// </summary>
    Queryable
}