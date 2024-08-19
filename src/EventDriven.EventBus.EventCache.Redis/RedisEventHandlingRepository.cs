using System.Text.Json;
using EventDriven.EventBus.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

namespace EventDriven.EventBus.EventCache.Redis;

/// <summary>
/// Redis event handling repository.
/// </summary>
public class RedisEventHandlingRepository<TIntegrationEvent> :
    IEventHandlingRepository<TIntegrationEvent>
    where TIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// Distributed cache.
    /// </summary>
    protected readonly IDistributedCache Cache;
    
    /// <summary>
    /// Distributed cache entry options.
    /// </summary>
    protected readonly DistributedCacheEntryOptions CacheOptions;

    /// <summary>
    /// Constructor.
    /// </summary>
    public RedisEventHandlingRepository(IDistributedCache cache,
        DistributedCacheEntryOptions cacheOptions)
    {
        Cache = cache;
        CacheOptions = cacheOptions;
    }

    /// <inheritdoc />
    public async Task<EventWrapper<TIntegrationEvent>?> GetEventAsync(string appName, string eventId,
        CancellationToken cancellationToken = default)
    {
        var id = $"{appName.ToLower()}||{eventId}";
        var json = await Cache.GetStringAsync(id, token: cancellationToken);
        if (string.IsNullOrEmpty(json)) return null;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return new EventWrapper<TIntegrationEvent>
        {
            Id = id,
            Value = JsonSerializer.Deserialize<EventHandling<TIntegrationEvent>>(json, options)
        };
    }
    
    /// <inheritdoc />
    public async Task<EventWrapper<TIntegrationEvent>> AddEventAsync(string appName, string eventId,
        EventHandling eventHandling, CancellationToken cancellationToken = default) =>
        await AddOrUpdateEventImplAsync(appName, eventId, eventHandling, cancellationToken);

    /// <inheritdoc />
    public async Task<EventWrapper<TIntegrationEvent>> UpdateEventAsync(string appName, string eventId,
        EventHandling eventHandling, CancellationToken cancellationToken = default) =>
        await AddOrUpdateEventImplAsync(appName, eventId, eventHandling, cancellationToken);

    /// <inheritdoc />
    public async Task<EventWrapper<TIntegrationEvent>> AddOrUpdateEventAsync(string appName, string eventId,
        EventHandling eventHandling, CancellationToken cancellationToken = default) =>
        await AddOrUpdateEventImplAsync(appName, eventId, eventHandling, cancellationToken);

    /// <inheritdoc />
    public async Task DeleteEventAsync(string appName, string eventId,
        CancellationToken cancellationToken = default) =>
        await Cache.RemoveAsync($"{appName.ToLower()}||{eventId}", cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<EventWrapper<TIntegrationEvent>>> GetExpiredEventsAsync(
        string? appName, bool excludeErrors, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    private async Task<EventWrapper<TIntegrationEvent>> AddOrUpdateEventImplAsync(string appName, string eventId,
        EventHandling eventHandling, CancellationToken cancellationToken = default)
    {
        var id = $"{appName.ToLower()}||{eventId}";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var @event = new EventWrapperDto
        {
            Id = id,
            Value = JsonSerializer.Serialize(eventHandling, options)
        };

        var json = JsonSerializer.Serialize(@event);
        await Cache.SetStringAsync(id, json, CacheOptions, cancellationToken);
        
        return new EventWrapper<TIntegrationEvent>
        {
            Id = @event.Id,
            Value = JsonSerializer.Deserialize<EventHandling<TIntegrationEvent>>(@event.Value, options)
        };
    }
}