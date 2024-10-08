using EventDriven.EventBus.Abstractions;
using EventDriven.EventBus.EventCache.Core;
using Microsoft.Extensions.Options;

namespace EventDriven.EventBus.EventCache.Mongo;

/// <inheritdoc />
public class MongoEventCache : CoreEventCache
{
    /// <inheritdoc />
    public MongoEventCache(IOptions<EventCacheOptions> eventCacheOptions,
        IEventHandlingRepository<IntegrationEvent> eventHandlingRepository,
        CancellationToken cancellationToken = default)
        : base(eventCacheOptions, eventHandlingRepository, cancellationToken)
    {
    }
}