using AsyncKeyedLock;
using EventDriven.EventBus.Abstractions;
using Microsoft.Extensions.Options;

namespace EventDriven.EventBus.EventCache.Mongo;

/// <inheritdoc />
public class MongoEventCache : IEventCache
{
    private readonly AsyncNonKeyedLocker _syncRoot = new();
    private readonly MongoEventCacheOptions _eventCacheOptions;
    private readonly IEventHandlingRepository<IntegrationEvent> _eventHandlingRepository;

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
    /// <param name="eventCacheOptions">Event cache options.</param>
    /// <param name="eventHandlingRepository">Event handling repository.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public MongoEventCache(
        IOptions<MongoEventCacheOptions> eventCacheOptions,
        IEventHandlingRepository<IntegrationEvent> eventHandlingRepository,
        CancellationToken cancellationToken = default)
    {
        _eventCacheOptions = eventCacheOptions.Value;
        _eventHandlingRepository = eventHandlingRepository;
        CancellationToken = cancellationToken;
        if (_eventCacheOptions.EnableEventCacheCleanup)
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
        using (await _syncRoot.LockAsync(CancellationToken))
        {
            // End timer and exit if cache cleanup disabled or cancellation pending
            if (!_eventCacheOptions.EnableEventCacheCleanup
                || CancellationToken.IsCancellationRequested)
            {
                if (CleanupTimer != null) await CleanupTimer.DisposeAsync();
                return;
            }
            
            // Remove expired events without errors
            var events = await _eventHandlingRepository.GetExpiredEventsAsync(
                _eventCacheOptions.AppName, true, CancellationToken);
            foreach (var @event in events)
            {
                if (@event.Value != null)
                    await _eventHandlingRepository.DeleteEventAsync(_eventCacheOptions.AppName,
                        @event.Value.EventId, CancellationToken);
            }
        }
    }

    /// <summary>
    /// Clean up event cache errors.
    /// </summary>
    /// <returns>Task that will complete when the operation has completed.</returns>
    protected virtual async Task CleanupEventCacheErrorsAsync()
    {
        using (await _syncRoot.LockAsync(CancellationToken))
        {
            // End timer and exit if cache cleanup disabled or cancellation pending
            if (!_eventCacheOptions.EnableEventCacheCleanup
                || CancellationToken.IsCancellationRequested)
            {
                if (ErrorsCleanupTimer != null) await ErrorsCleanupTimer.DisposeAsync();
                return;
            }
            
            // Remove expired events with errors
            var events = await _eventHandlingRepository.GetExpiredEventsAsync(
                _eventCacheOptions.AppName, false, CancellationToken);
            foreach (var @event in events)
            {
                if (@event.Value != null)
                    await _eventHandlingRepository.DeleteEventAsync(_eventCacheOptions.AppName,
                        @event.Value.EventId, CancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasBeenHandledAsync(IntegrationEvent @event, string handlerTypeName)
    {
        using (await _syncRoot.LockAsync(CancellationToken))
        {
            // Return false if cache not enabled
            if (!_eventCacheOptions.EnableEventCache) return false;

            // Return true if event exists, is not expired, handler is started or completed and has no error
            var wrapper = await _eventHandlingRepository.GetEventAsync(
                _eventCacheOptions.AppName, @event.Id, CancellationToken);
            return wrapper is not null && HasBeenHandledImpl(wrapper, handlerTypeName);
        }
    }

    /// <inheritdoc />
    public async Task AddEventAsync(IntegrationEvent @event,
        string? handlerTypeName = null, string? errorMessage = null)
    {
        using (await _syncRoot.LockAsync(CancellationToken))
        {
            // Return if cache not enabled
            if (!_eventCacheOptions.EnableEventCache) return;
        
            // Retrieve existing event
            var wrapper = await _eventHandlingRepository.GetEventAsync(
                _eventCacheOptions.AppName, @event.Id, CancellationToken);
            
            // Update event handling as completed
            var handling = UpdateEventHandling(wrapper, @event, handlerTypeName, null, HandlerState.Completed);

            // Add event to repository
            await _eventHandlingRepository.AddEventAsync(_eventCacheOptions.AppName,
                @event.Id, handling, CancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task UpdateEventAsync(IntegrationEvent @event, string? handlerTypeName = null, string? errorMessage = null)
    {
        using (await _syncRoot.LockAsync(CancellationToken))
        {
            // Return if cache not enabled
            if (!_eventCacheOptions.EnableEventCache) return;
        
            // Retrieve existing event
            var wrapper = await _eventHandlingRepository.GetEventAsync(
                _eventCacheOptions.AppName, @event.Id, CancellationToken);
            
            // Update event handling as completed
            var handling = UpdateEventHandling(wrapper, @event, handlerTypeName, null, HandlerState.Completed);

            // Update event in repository
            await _eventHandlingRepository.UpdateEventAsync(_eventCacheOptions.AppName,
                @event.Id, handling, CancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasBeenHandledPersistEventAsync(IntegrationEvent @event, string? handlerTypeName = null)
    {
        using (await _syncRoot.LockAsync(CancellationToken))
        {
            // Get event; return true if handled
            var wrapper = await _eventHandlingRepository.GetEventAsync(
                _eventCacheOptions.AppName, @event.Id, CancellationToken);
            if (wrapper is not null && handlerTypeName is not null
                && HasBeenHandledImpl(wrapper, handlerTypeName))
                return true;

            // Update event handling as started
            var handling = UpdateEventHandling(wrapper, @event, handlerTypeName, null, HandlerState.Started);

            // Add event to repository
            await _eventHandlingRepository.AddOrUpdateEventAsync(_eventCacheOptions.AppName,
                @event.Id, handling, CancellationToken);
            return false;
        }
    }

    private bool HasBeenHandledImpl(EventWrapper<IntegrationEvent> wrapper, string handlerTypeName)
    {
        var exists = wrapper?.Value != null;
        var expired =
            wrapper?.Value != null &&
            DateTime.UtcNow > wrapper.Value.EventHandledTime + wrapper.Value.EventHandledTimeout;
        var state = wrapper?.Value?.Handlers[handlerTypeName].HanderState ?? HandlerState.NotStarted;
        var hasError =
            wrapper?.Value != null &&
            wrapper.Value.Handlers.ContainsKey(handlerTypeName) &&
            wrapper.Value.Handlers[handlerTypeName].HasError;
        var hasBeenHandled = exists && !(expired || state == HandlerState.NotStarted || hasError);
        return hasBeenHandled;
    }

    private EventHandling<IntegrationEvent> UpdateEventHandling(EventWrapper<IntegrationEvent>? wrapper,
        IntegrationEvent @event, string? handlerTypeName, string? errorMessage, HandlerState handlerState)
    {
        // Reference or create event handling
        var handling = wrapper?.Value ?? new EventHandling<IntegrationEvent>
        {
            EventId = @event.Id,
            IntegrationEvent = @event,
            EventHandledTime = DateTime.UtcNow,
            EventHandledTimeout = _eventCacheOptions.EventCacheTimeout
        };

        // Set handler state to complete; set error if there is one
        if (!string.IsNullOrWhiteSpace(handlerTypeName))
        {
            if (handling.Handlers.TryGetValue(handlerTypeName, out var handler))
                handler.HanderState = handlerState;
            else
                handling.Handlers.Add(handlerTypeName, new HandlerInfo
                {
                    HanderState = handlerState,
                    HasError = !string.IsNullOrWhiteSpace(errorMessage),
                    ErrorMessage = !string.IsNullOrWhiteSpace(errorMessage)
                        ? errorMessage : null
                });
        }
        return handling;
    }
}