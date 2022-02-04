using System;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using EventDriven.EventBus.Abstractions;

namespace EventDriven.EventBus.Dapr;

/// <inheritdoc />
public class DaprEventCache : IDaprEventCache
{
    private readonly DaprClient _dapr;
    private readonly SemaphoreSlim _syncRoot;

    /// <inheritdoc />
    public DaprEventBusOptions DaprEventBusOptions { get; set; }
    
    /// <summary>
    /// Lock timeout.
    /// </summary>
    protected TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dapr">Dapr client.</param>
    /// <param name="options">Dapr event bus options.</param>
    public DaprEventCache(DaprClient dapr, DaprEventBusOptions options)
    {
        _dapr = dapr;
        _syncRoot = new SemaphoreSlim(1, 1);
        DaprEventBusOptions = options;
    }

    /// <inheritdoc />
    public async Task<bool> TryAddAsync(IIntegrationEvent @event)
    {
        try
        {
            // Return true if not enabled
            await _syncRoot.WaitAsync(LockTimeout);
            if (!DaprEventBusOptions.DaprEventCacheOptions.EnableEventCache) return true;
        
            // Return false if event exists and is not expired
            var existing = await _dapr.GetStateAsync<EventHandling>(DaprEventBusOptions.DaprEventCacheOptions.DaprStateStoreOptions.StateStoreName, @event.Id);
            if (existing != null && existing.EventHandledTimeout < DateTime.UtcNow - existing.EventHandledTime)
                return false;
        
            // Remove existing if event is expired
            if (existing != null)
                await _dapr.DeleteStateAsync(DaprEventBusOptions.DaprEventCacheOptions.DaprStateStoreOptions.StateStoreName, @event.Id);
            
            // Add event handling
            var handling = new EventHandling
            {
                EventId = @event.Id,
                IntegrationEvent = @event,
                EventHandledTime = DateTime.UtcNow,
                EventHandledTimeout = DaprEventBusOptions.DaprEventCacheOptions.EventCacheTimeout
            };
            await _dapr.SaveStateAsync(DaprEventBusOptions.DaprEventCacheOptions.DaprStateStoreOptions.StateStoreName, @event.Id, handling);
            return true;
        }
        finally
        {
            _syncRoot.Release();
        }
    }
}