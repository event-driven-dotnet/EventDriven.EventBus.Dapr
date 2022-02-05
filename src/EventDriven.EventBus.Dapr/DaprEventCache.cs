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
    private readonly IEventHandlingRepository _eventHandlingRepository;

    /// <summary>
    /// Lock timeout.
    /// </summary>
    protected TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Cleanup timer.
    /// </summary>
    protected Timer CleanupTimer { get; }

    /// <inheritdoc />
    public DaprEventBusOptions DaprEventBusOptions { get; set; }
    
    /// <summary>
    /// Cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dapr">Dapr client.</param>
    /// <param name="options">Dapr event bus options.</param>
    /// <param name="eventHandlingRepository"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public DaprEventCache(
        DaprClient dapr, 
        DaprEventBusOptions options,
        IEventHandlingRepository eventHandlingRepository,
        CancellationToken cancellationToken = default)
    {
        _dapr = dapr;
        _eventHandlingRepository = eventHandlingRepository;
        _syncRoot = new SemaphoreSlim(1, 1);
        DaprEventBusOptions = options;
        CancellationToken = cancellationToken;
        async void TimerCallback(object state) => await CleanupEventCacheAsync();
        if (DaprEventBusOptions.DaprEventCacheOptions.EnableEventCacheCleanup)
            CleanupTimer = new Timer(TimerCallback, null, TimeSpan.Zero, 
                DaprEventBusOptions.DaprEventCacheOptions.EventCacheCleanupInterval);
    }

    /// <summary>
    /// Cleans up event cache.
    /// </summary>
    /// <returns>Task that will complete when the operation has completed.</returns>
    protected virtual async Task CleanupEventCacheAsync()
    {
        try
        {
            // End timer and exit if cache cleanup disabled or cancellation pending
            await _syncRoot.WaitAsync(LockTimeout, CancellationToken);
            if (!DaprEventBusOptions.DaprEventCacheOptions.EnableEventCacheCleanup
                || CancellationToken.IsCancellationRequested)
            {
                await CleanupTimer.DisposeAsync();
                return;
            }
            
            // Remove expired events
            await _eventHandlingRepository.RemoveExpiredEventsAsync();
        }
        finally
        {
            _syncRoot.Release();
        }
    }

    /// <inheritdoc />
    public virtual async Task<bool> TryAddAsync(IIntegrationEvent @event)
    {
        try
        {
            // Return true if not enabled
            await _syncRoot.WaitAsync(LockTimeout, CancellationToken);
            if (!DaprEventBusOptions.DaprEventCacheOptions.EnableEventCache) return true;
        
            // Return false if event exists and is not expired
            var existing = await _dapr.GetStateAsync<EventHandling>(DaprEventBusOptions.DaprEventCacheOptions
                .DaprStateStoreOptions.StateStoreName, @event.Id, null, null, CancellationToken);
            if (existing != null && existing.EventHandledTimeout < DateTime.UtcNow - existing.EventHandledTime)
                return false;
        
            // Remove existing if event is expired
            if (existing != null)
                await _dapr.DeleteStateAsync(DaprEventBusOptions.DaprEventCacheOptions
                    .DaprStateStoreOptions.StateStoreName, @event.Id, null, null, CancellationToken);
            
            // Add event handling
            var handling = new EventHandling
            {
                EventId = @event.Id,
                IntegrationEvent = @event,
                EventHandledTime = DateTime.UtcNow,
                EventHandledTimeout = DaprEventBusOptions.DaprEventCacheOptions.EventCacheTimeout
            };
            await _dapr.SaveStateAsync(DaprEventBusOptions.DaprEventCacheOptions
                .DaprStateStoreOptions.StateStoreName, @event.Id, handling, null, null, CancellationToken);
            return true;
        }
        finally
        {
            _syncRoot.Release();
        }
    }
}