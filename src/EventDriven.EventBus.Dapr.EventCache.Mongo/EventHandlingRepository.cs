using System.Text.Json;
using EventDriven.EventBus.Abstractions;
using URF.Core.Abstractions;
using URF.Core.Mongo;

namespace EventDriven.EventBus.Dapr.EventCache.Mongo;

/// <summary>
/// Event handing repository.
/// </summary>
/// <typeparam name="TIntegrationEvent">Integration event type.</typeparam>
public class EventHandlingRepository<TIntegrationEvent> : IEventHandlingRepository<TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    private readonly IDocumentRepository<EventWrapperDto> _documentRepository;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="documentRepository">Document repository.</param>
    public EventHandlingRepository(
        IDocumentRepository<EventWrapperDto> documentRepository)
    {
        _documentRepository = documentRepository;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EventWrapper<IIntegrationEvent>>> GetExpiredEventsAsync()
    {
        var dtos = await GetEventWrapperDtosAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var expired = dtos
            .Select(e =>
                new EventWrapper<IIntegrationEvent>
                {
                    Id = e.Id,
                    Etag = e.Etag,
                    Value = JsonSerializer.Deserialize<EventHandling<IIntegrationEvent>>(e.Value, options)
                })
            .Where(e => e.Value != null && e.Value.EventHandledTimeout < 
                DateTime.UtcNow - e.Value.EventHandledTime)
            .ToList();
        return expired;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EventWrapper<TIntegrationEvent>>> GetExpiredIntegrationEventsAsync()
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
            var result = await _documentRepository.DeleteOneAsync(e => e.Id == wrapper.Id);
            deleted += result;
        }
        return deleted;
    }
    
    private async Task<IEnumerable<EventWrapperDto>> GetEventWrapperDtosAsync() =>
        await _documentRepository
            .Queryable()
            .ToListAsync();
}