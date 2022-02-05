using System.Collections.Generic;
using System.Threading.Tasks;
using EventDriven.EventBus.Abstractions;

namespace EventDriven.EventBus.Dapr;

/// <inheritdoc />
public class NullEventHandlingRepository<TIntegrationEvent> : IEventHandlingRepository<TIntegrationEvent>
    where TIntegrationEvent : IntegrationEvent
{
    /// <inheritdoc />
    public Task<IEnumerable<EventWrapper<TIntegrationEvent>>> GetExpiredEventsAsync()
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public Task<int> RemoveExpiredEventsAsync()
    {
        throw new System.NotImplementedException();
    }
}