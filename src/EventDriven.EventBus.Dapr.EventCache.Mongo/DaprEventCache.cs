using AsyncKeyedLock;
using Dapr.Client;
using EventDriven.EventBus.Abstractions;
using EventDriven.EventBus.EventCache.Mongo;
using Microsoft.Extensions.Options;

namespace EventDriven.EventBus.Dapr.EventCache.Mongo;

/// <inheritdoc />
public class DaprEventCache : IEventCache
{
    private readonly DaprClient _dapr;
    private readonly AsyncNonKeyedLocker _syncRoot = new();
    private readonly DaprEventCacheOptions _eventCacheOptions;
    private readonly IEventHandlingRepository<DaprIntegrationEvent> _eventHandlingRepository;
    
    /// <summary>
    /// Cleanup timer.
    /// </summary>
    protected Timer? CleanupTimer { get; }

    /// <summary>
    /// Errors cleanup timer.
    /// </summary>
    protected Timer? ErrorsCleanupTimer { get; }

    /// <summary>
    /// Cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dapr">Dapr client.</param>
    /// <param name="eventCacheOptions">Dapr event cache options.</param>
    /// <param name="eventHandlingRepository">Event handling repository.</param>
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
        if (_eventCacheOptions.DaprEventCacheType == DaprEventCacheType.Queryable &&
            _eventCacheOptions.EnableEventCacheCleanup)
        {
            CleanupTimer = new Timer(TimerCallback!, null, TimeSpan.Zero, 
                _eventCacheOptions.EventCacheCleanupInterval);
            ErrorsCleanupTimer = new Timer(ErrorsTimerCallback, null, TimeSpan.Zero, 
                _eventCacheOptions.EventErrorsCacheCleanupInterval);
        }
        return;
        async void TimerCallback(object state) => await CleanupEventCacheAsync();
        async void ErrorsTimerCallback(object? state) => await CleanupEventCacheErrorsAsync();
    }

    /// <summary>
    /// Clean up event cache.
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
            
            // Remove expired events without errors
            var events = 
                await _eventHandlingRepository.GetExpiredEventsAsync(null, true, CancellationToken);
            foreach (var @event in events)
            {
                if (@event.Value != null)
                    await _dapr.DeleteStateAsync(_eventCacheOptions.DaprStateStoreOptions.StateStoreName,
                        @event.Value.EventId, null, null, CancellationToken);
            }
        }
    }

    /// <summary>
    /// Clean up event cache errors.
    /// </summary>
    /// <returns>Task that will complete when the operation has completed.</returns>
    protected virtual async Task CleanupEventCacheErrorsAsync()
    {
        using (await _syncRoot.LockAsync())
        {
            // End timer and exit if cache cleanup disabled or cancellation pending
            if (!(_eventCacheOptions.DaprEventCacheType == DaprEventCacheType.Queryable &&
                  _eventCacheOptions.EnableEventCacheCleanup)
                || CancellationToken.IsCancellationRequested)
            {
                if (ErrorsCleanupTimer != null) await ErrorsCleanupTimer.DisposeAsync();
                return;
            }
            
            // Remove expired events with errors
            var events = 
                await _eventHandlingRepository.GetExpiredEventsAsync(null, false, CancellationToken);
            foreach (var @event in events)
            {
                if (@event.Value != null)
                    await _dapr.DeleteStateAsync(_eventCacheOptions.DaprStateStoreOptions.StateStoreName,
                        @event.Value.EventId, null, null, CancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasBeenHandledAsync(IntegrationEvent @event, string handlerTypeName)
    {
        // Return false if not enabled
        if (!_eventCacheOptions.EnableEventCache) return false;
        
        // Return true if event exists, is not expired, and handler has no error
        var wrapper = await _dapr.GetStateAsync<EventHandling>(_eventCacheOptions
            .DaprStateStoreOptions.StateStoreName, @event.Id, null, null, CancellationToken);
        var exists = wrapper != null;
        var expired =
            wrapper != null &&
            DateTime.UtcNow > wrapper.EventHandledTime + wrapper.EventHandledTimeout;
        var hasError =
            wrapper != null &&
            wrapper.Handlers.ContainsKey(handlerTypeName) &&
            wrapper.Handlers[handlerTypeName].HasError;
        var hasBeenHandled = exists && !(expired || hasError);
        return hasBeenHandled;
    }

    /// <inheritdoc />
    public virtual async Task AddEventAsync(IntegrationEvent @event,
        string? handlerTypeName = null, string? errorMessage = null)
    {
        using (await _syncRoot.LockAsync())
        {
            // Return if cache not enabled
            if (!_eventCacheOptions.EnableEventCache) return;
        
            // Remove existing event
            await _dapr.DeleteStateAsync(_eventCacheOptions
                .DaprStateStoreOptions.StateStoreName, @event.Id, null, null, CancellationToken);
            
            // Add new event
            var handling = new EventHandling
            {
                EventId = @event.Id,
                IntegrationEvent = @event,
                EventHandledTime = DateTime.UtcNow,
                EventHandledTimeout = _eventCacheOptions.EventCacheTimeout
            };
            if (!string.IsNullOrWhiteSpace(handlerTypeName))
            {
                handling.Handlers.Add(handlerTypeName, new HandlerInfo
                {
                    HasError = !string.IsNullOrWhiteSpace(errorMessage),
                    ErrorMessage = !string.IsNullOrWhiteSpace(errorMessage)
                        ? errorMessage : null
                });
            }

            await _dapr.SaveStateAsync(_eventCacheOptions.DaprStateStoreOptions.StateStoreName,
                @event.Id, handling, null, null, CancellationToken);
        }
    }
}