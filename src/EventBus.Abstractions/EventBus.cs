using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventBus.Abstractions
{
    ///<inheritdoc/>
    public abstract class EventBus : IEventBus
    {
        ///<inheritdoc/>
        public Dictionary<string, List<IIntegrationEventHandler>> Topics { get; } = new();

        ///<inheritdoc/>
        public virtual void Subscribe(IIntegrationEventHandler handler, string topic = null)
        {
            var topicName = topic ?? handler.Topic;
            if (Topics.TryGetValue(topicName, out var handlers))
            {
                handlers.Add(handler);
            }
            else
            {
                Topics.Add(topicName, new List<IIntegrationEventHandler> { handler });
            }
        }

        ///<inheritdoc/>
        public abstract Task PublishAsync<TIntegrationEvent>(TIntegrationEvent @event, string topic = null)
            where TIntegrationEvent : IIntegrationEvent;
    }
}
