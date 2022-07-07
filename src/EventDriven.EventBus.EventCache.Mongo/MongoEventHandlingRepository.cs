using System.Text.Json;
using EventDriven.EventBus.Abstractions;
using MongoDB.Driver;
using URF.Core.Mongo;

namespace EventDriven.EventBus.EventCache.Mongo;

/// <summary>
/// Mongo event handling repository.
/// </summary>
public class MongoEventHandlingRepository<TIntegrationEvent> :
    DocumentRepository<EventWrapperDto>,
    IEventHandlingRepository<TIntegrationEvent>
    where TIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="collection">EventWrapperDto collection.</param>
    public MongoEventHandlingRepository(IMongoCollection<EventWrapperDto> collection) : base(collection)
    {
    }

    /// <inheritdoc />
    public async Task<EventWrapper<TIntegrationEvent>?> GetEventAsync(string appName, string eventId,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var dto = await FindOneAsync(e =>
            e.Id == $"{appName.ToLower()}||{eventId}", cancellationToken);
        if (dto == null) return null;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return new EventWrapper<TIntegrationEvent>
        {
            Id = dto.Id,
            Etag = dto.Etag,
            Value = JsonSerializer.Deserialize<EventHandling<TIntegrationEvent>>(dto.Value, options)
        };
    }

    /// <inheritdoc />
    public async Task AddEventAsync(string appName, string eventId,
        EventHandling eventHandling, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var @event = new EventWrapperDto
        {
            Id = $"{appName.ToLower()}||{eventId}",
            Etag = Guid.NewGuid().ToString(),
            Value = JsonSerializer.Serialize(eventHandling, options)
        };
        await InsertOneAsync(@event, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteEventAsync(string appName, string eventId,
        CancellationToken cancellationToken = new()) =>
        await DeleteOneAsync(e =>
            e.Id == $"{appName.ToLower()}||{eventId}", cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<EventWrapper<TIntegrationEvent>>> GetExpiredEventsAsync(
        string? appName = null, CancellationToken cancellationToken = default)
    {
        var dtos = await Queryable()
            .ToListAsync(cancellationToken);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var expired = dtos
            .Select(e =>
                new EventWrapper<TIntegrationEvent>
                {
                    Id = e.Id,
                    Etag = e.Etag,
                    Value = JsonSerializer.Deserialize<EventHandling<TIntegrationEvent>>(e.Value, options)
                })
            .Where(e => appName == null || e.Id.StartsWith(appName)
                && e.Value != null && e.Value.EventHandledTimeout < DateTime.UtcNow - e.Value.EventHandledTime)
            .ToList();
        return expired;
    }
}