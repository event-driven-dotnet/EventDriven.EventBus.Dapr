using Dapr.Client;
using EventDriven.EventBus.Abstractions;
using Microsoft.Extensions.Options;
using NeoSmart.AsyncLock;

namespace EventDriven.EventBus.Dapr.EventCache.Mongo;

/// <inheritdoc />
public class DaprEventCache : IEventCache
{
    private readonly DaprClient _dapr;
    private readonly AsyncLock _syncRoot = new();
    private readonly DaprEventCacheOptions _eventCacheOptions;
    private readonly IEventHandlingRepository<DaprIntegrationEvent> _eventHandlingRepository;
    
    /// <summary>
    /// Cleanup timer.
    /// </summary>
    protected Timer? CleanupTimer { get; }

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
        _eventCacheOptions = eventCacheOptions.Value;
        CancellationToken = cancellationToken;
        async void TimerCallback(object state) => await CleanupEventCacheAsync();
        if (_eventCacheOptions.DaprEventCacheType == DaprEventCacheType.Queryable &&
            _eventCacheOptions.EnableEventCacheCleanup)
            CleanupTimer = new Timer(TimerCallback!, null, TimeSpan.Zero, 
                _eventCacheOptions.EventCacheCleanupInterval);
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
            if (!(_eventCacheOptions.DaprEventCacheType == DaprEventCacheType.Queryable &&
                  _eventCacheOptions.EnableEventCacheCleanup)
                || CancellationToken.IsCancellationRequested)
            {
                if (CleanupTimer != null) await CleanupTimer.DisposeAsync();
                return;
            }
            
            // Remove expired events
            var events = await _eventHandlingRepository.GetExpiredEventsAsync();
            foreach (var @event in events)
            {
                if (@event.Value != null)
                    await _dapr.DeleteStateAsync(_eventCacheOptions.DaprStateStoreOptions.StateStoreName,
                        @event.Value.EventId, null, null, CancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public bool TryAdd(IntegrationEvent? @event) =>
        TryAddAsync(@event).Result;

    /// <inheritdoc />
    public virtual async Task<bool> TryAddAsync(IntegrationEvent? @event)
    {
        using (await _syncRoot.LockAsync())
        {
            // Return true if not enabled
            if (!_eventCacheOptions.EnableEventCache) return true;
        
            // Return false if event exists and is not expired
            var existing = await _dapr.GetStateAsync<EventHandling>(_eventCacheOptions
                .DaprStateStoreOptions.StateStoreName, @event?.Id, null, null, CancellationToken);
            if (existing != null && existing.EventHandledTimeout < DateTime.UtcNow - existing.EventHandledTime)
                return false;
        
            // Remove existing if event is expired
            if (existing != null)
                await _dapr.DeleteStateAsync(_eventCacheOptions
                    .DaprStateStoreOptions.StateStoreName, @event?.Id, null, null, CancellationToken);
            
            // Add event handling
            var handling = new EventHandling
            {
                EventId = @event?.Id!,
                IntegrationEvent = @event!,
                EventHandledTime = DateTime.UtcNow,
                EventHandledTimeout = _eventCacheOptions.EventCacheTimeout
            };
            await _dapr.SaveStateAsync(_eventCacheOptions.DaprStateStoreOptions.StateStoreName,
                @event?.Id, handling, null, null, CancellationToken);
            return true;
        }
    }
}