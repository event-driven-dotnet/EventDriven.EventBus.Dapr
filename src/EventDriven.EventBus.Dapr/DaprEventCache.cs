using System;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using EventDriven.EventBus.Abstractions;
using Microsoft.Extensions.Options;
using NeoSmart.AsyncLock;

namespace EventDriven.EventBus.Dapr;

/// <inheritdoc />
public class DaprEventCache : IDaprEventCache
{
    private readonly DaprClient _dapr;
    private readonly AsyncLock _syncRoot = new();
    private readonly IEventHandlingRepository<DaprIntegrationEvent> _eventHandlingRepository;

    /// <summary>
    /// Cleanup timer.
    /// </summary>
    protected Timer CleanupTimer { get; }

    /// <inheritdoc />
    public DaprEventCacheOptions DaprEventCacheOptions { get; set; }
    
    /// <summary>
    /// Cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dapr">Dapr client.</param>
    /// <param name="eventCacheOptions">Dapr event cache options.</param>
    /// <param name="eventHandlingRepository"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public DaprEventCache(
        DaprClient dapr, 
        IOptions<DaprEventCacheOptions> eventCacheOptions,
        IEventHandlingRepository<DaprIntegrationEvent> eventHandlingRepository,
        CancellationToken cancellationToken = default)
    {
        _dapr = dapr;
        _eventHandlingRepository = eventHandlingRepository;
        DaprEventCacheOptions = eventCacheOptions.Value;
        CancellationToken = cancellationToken;
        async void TimerCallback(object state) => await CleanupEventCacheAsync();
        if (DaprEventCacheOptions.DaprEventCacheType == DaprEventCacheType.Queryable &&
            DaprEventCacheOptions.EnableEventCacheCleanup)
            CleanupTimer = new Timer(TimerCallback, null, TimeSpan.Zero, 
                DaprEventCacheOptions.EventCacheCleanupInterval);
    }

    /// <summary>
    /// Cleans up event cache.
    /// </summary>
    /// <returns>Task that will complete when the operation has completed.</returns>
    protected virtual async Task CleanupEventCacheAsync()
    {
        using (await _syncRoot.LockAsync())
        {
            // End timer and exit if cache cleanup disabled or cancellation pending
            if (!(DaprEventCacheOptions.DaprEventCacheType == DaprEventCacheType.Queryable &&
                  DaprEventCacheOptions.EnableEventCacheCleanup)
                || CancellationToken.IsCancellationRequested)
            {
                await CleanupTimer.DisposeAsync();
                return;
            }
            
            // Remove expired events
            var events = await _eventHandlingRepository.GetExpiredEventsAsync();
            foreach (var @event in events)
            {
                if (@event.Value != null)
                    await _dapr.DeleteStateAsync(DaprEventCacheOptions.DaprStateStoreOptions.StateStoreName,
                        @event.Value.EventId, null, null, CancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public virtual async Task<bool> TryAddAsync(IntegrationEvent @event)
    {
        using (await _syncRoot.LockAsync())
        {
            // Return true if not enabled
            if (!DaprEventCacheOptions.EnableEventCache) return true;
        
            // Return false if event exists and is not expired
            var existing = await _dapr.GetStateAsync<EventHandling>(DaprEventCacheOptions
                .DaprStateStoreOptions.StateStoreName, @event.Id, null, null, CancellationToken);
            if (existing != null && existing.EventHandledTimeout < DateTime.UtcNow - existing.EventHandledTime)
                return false;
        
            // Remove existing if event is expired
            if (existing != null)
                await _dapr.DeleteStateAsync(DaprEventCacheOptions
                    .DaprStateStoreOptions.StateStoreName, @event.Id, null, null, CancellationToken);
            
            // Add event handling
            var handling = new EventHandling
            {
                EventId = @event.Id,
                IntegrationEvent = @event,
                EventHandledTime = DateTime.UtcNow,
                EventHandledTimeout = DaprEventCacheOptions.EventCacheTimeout
            };
            await _dapr.SaveStateAsync(DaprEventCacheOptions.DaprStateStoreOptions.StateStoreName,
                @event.Id, handling, null, null, CancellationToken);
            return true;
        }
    }
}