using System.Text.Json;
using EventDriven.EventBus.Abstractions;
using URF.Core.Abstractions;
using URF.Core.Mongo;

namespace EventDriven.EventBus.Dapr.EventCache.Mongo;

/// <summary>
/// Mongo event handling repository.
/// </summary>
public class MongoEventHandlingRepository<TIntegrationEvent> : IEventHandlingRepository<TIntegrationEvent>
    where TIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// Document repository.
    /// </summary>
    protected readonly IDocumentRepository<EventWrapperDto> DocumentRepository;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="documentRepository">Document repository.</param>
    public MongoEventHandlingRepository(
        IDocumentRepository<EventWrapperDto> documentRepository)
    {
        DocumentRepository = documentRepository;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EventWrapper<TIntegrationEvent>>> GetExpiredEventsAsync()
    {
        var dtos = await GetEventWrapperDtosAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var expired = dtos
            .Select(e =>
                new EventWrapper<TIntegrationEvent>
                {
                    Id = e.Id,
                    Etag = e.Etag,
                    Value = JsonSerializer.Deserialize<EventHandling<TIntegrationEvent>>(e.Value, options)
                })
            .Where(e => e.Value != null && e.Value.EventHandledTimeout < 
                DateTime.UtcNow - e.Value.EventHandledTime)
            .ToList();
        return expired;
    }

    /// <inheritdoc />
    public async Task<int> RemoveExpiredEventsAsync()
    {
        int deleted = 0;
        var expired = await GetExpiredEventsAsync();
        foreach (var wrapper in expired)
        {
            var result = await DocumentRepository.DeleteOneAsync(e => e.Id == wrapper.Id);
            deleted += result;
        }
        return deleted;
    }

    /// <summary>
    /// Get event wrapper DTO's.
    /// </summary>
    /// <returns>
    /// Task that will complete when the operation has completed.
    /// Task contains an IEnumerable of EventWrapper DTO's.
    /// </returns>
    protected async Task<IEnumerable<EventWrapperDto>> GetEventWrapperDtosAsync() =>
        await DocumentRepository
            .Queryable()
            .ToListAsync();
}