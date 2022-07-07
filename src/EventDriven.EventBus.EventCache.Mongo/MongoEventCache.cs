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
        async void TimerCallback(object state) => await CleanupEventCacheAsync();
        if (_eventCacheOptions.EnableEventCacheCleanup)
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
            if (!_eventCacheOptions.EnableEventCacheCleanup
                || CancellationToken.IsCancellationRequested)
            {
                if (CleanupTimer != null) await CleanupTimer.DisposeAsync();
                return;
            }
            
            // Remove expired events
            var events = await _eventHandlingRepository.GetExpiredEventsAsync(
                _eventCacheOptions.AppName, CancellationToken);
            foreach (var @event in events)
            {
                if (@event.Value != null)
                    await _eventHandlingRepository.DeleteEventAsync(_eventCacheOptions.AppName,
                        @event.Value.EventId, CancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public bool TryAdd(IntegrationEvent @event) =>
        TryAddAsync(@event).Result;

    /// <inheritdoc />
    public async Task<bool> TryAddAsync(IntegrationEvent @event)
    {
        using (await _syncRoot.LockAsync())
        {
            // Return true if not enabled
            if (!_eventCacheOptions.EnableEventCache) return true;
        
            // Return false if event exists and is not expired
            var existing = await _eventHandlingRepository.GetEventAsync(
                _eventCacheOptions.AppName, @event.Id, CancellationToken);
            if (existing?.Value != null && existing.Value.EventHandledTimeout < DateTime.UtcNow - existing.Value.EventHandledTime)
                return false;
        
            // Remove existing if event is expired
            await _eventHandlingRepository.DeleteEventAsync(_eventCacheOptions.AppName, @event.Id, CancellationToken);
            
            // Add event handling
            var handling = new EventHandling
            {
                EventId = @event.Id,
                IntegrationEvent = @event,
                EventHandledTime = DateTime.UtcNow,
                EventHandledTimeout = _eventCacheOptions.EventCacheTimeout
            };
            await _eventHandlingRepository.AddEventAsync(_eventCacheOptions.AppName, @event.Id,
                handling, CancellationToken);
            return true;
        }
    }
}