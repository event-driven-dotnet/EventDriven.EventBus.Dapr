using System.Threading.Tasks;
using EventDriven.EventBus.Abstractions;

namespace EventDriven.EventBus.Dapr;

/// <summary>
/// Dapr event cache to enable idempotency.
/// </summary>
public interface IDaprEventCache
{
    /// <summary>
    /// Dapr event bus options.
    /// </summary>
    DaprEventBusOptions DaprEventBusOptions { get; set; }
    
    /// <summary>
    /// Attempts to add the integration event to the event cache.
    /// </summary>
    /// <param name="event">The integration event</param>
    /// <returns>
    /// Task that will complete when the operation has completed.
    /// Task contains true if the event was added to the event cache,
    /// false if the event is in the cache and not expired or it cannot be removed.
    /// </returns>
    Task<bool> TryAddAsync(IIntegrationEvent @event);
}