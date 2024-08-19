using EventDriven.EventBus.Abstractions;
using EventDriven.EventBus.EventCache.Core;
using Microsoft.Extensions.Options;

namespace EventDriven.EventBus.EventCache.Redis;

/// <inheritdoc />
public class RedisEventCache : CoreEventCache
{
    /// <inheritdoc />
    public RedisEventCache(IOptions<EventCacheOptions> eventCacheOptions,
        IEventHandlingRepository<IntegrationEvent> eventHandlingRepository,
        CancellationToken cancellationToken = default)
        : base(eventCacheOptions, eventHandlingRepository, cancellationToken)
    {
    }

    /// <inheritdoc />
    protected override Task CleanupEventCacheAsync() => Task.CompletedTask;

    /// <inheritdoc />
    protected override Task CleanupEventCacheErrorsAsync() => Task.CompletedTask;
}