using EventDriven.EventBus.Abstractions;
using Microsoft.Extensions.Options;
using NeoSmart.AsyncLock;

namespace EventDriven.EventBus.EventCache.Mongo;

/// <inheritdoc />
public class MongoEventCache : IEventCache
{
    private readonly AsyncLock _syncRoot = new();
    private readonly MongoEventCacheOptions _eventCacheOptions;
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
    /// <param name="eventCacheOptions">Event cache options.</param>
    /// <param name="eventHandlingRepository">Event handling repository.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public MongoEventCache(
        IOptions<MongoEventCacheOptions> eventCacheOptions,
        IEventHandlingRepository<DaprIntegrationEvent> eventHandlingRepository,
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
        using (await _syncRoot.LockAsync())
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
        using (await _syncRoot.LockAsync())
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
        using (await _syncRoot.LockAsync())
        {
            // Return false if cache not enabled
            if (!_eventCacheOptions.EnableEventCache) return false;

            // Return true if event exists, is not expired, and handler has no error
            var wrapper = await _eventHandlingRepository.GetEventAsync(
                _eventCacheOptions.AppName, @event.Id, CancellationToken);
            var exists = wrapper?.Value != null;
            var expired =
                wrapper?.Value != null &&
                DateTime.UtcNow > wrapper.Value.EventHandledTime + wrapper.Value.EventHandledTimeout;
            var hasError =
                wrapper?.Value != null &&
                wrapper.Value.Handlers.ContainsKey(handlerTypeName) &&
                wrapper.Value.Handlers[handlerTypeName].HasError;
            var hasBeenHandled = exists && !(expired || hasError);
            return hasBeenHandled;
        }
    }

    /// <inheritdoc />
    public async Task AddEventAsync(IntegrationEvent @event,
        string? handlerTypeName = null, string? errorMessage = null)
    {
        using (await _syncRoot.LockAsync())
        {
            // Return if cache not enabled
            if (!_eventCacheOptions.EnableEventCache) return;
        
            // Remove existing event
            await _eventHandlingRepository.DeleteEventAsync(_eventCacheOptions.AppName, @event.Id, CancellationToken);
            
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

            await _eventHandlingRepository.AddEventAsync(_eventCacheOptions.AppName, @event.Id,
                handling, CancellationToken);
        }
    }
}