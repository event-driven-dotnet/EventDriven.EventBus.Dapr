namespace EventDriven.EventBus.Dapr.EventCache.Mongo;

/// <summary>
/// Dapr state store options.
/// </summary>
public class DaprStateStoreOptions
{
    /// <summary>
    /// Dapr State Store component name.
    /// </summary>
    public string StateStoreName { get; set; } = "statestore";
}