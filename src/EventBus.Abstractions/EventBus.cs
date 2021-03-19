using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventBus.Abstractions
{
    ///<inheritdoc/>
    public abstract class EventBus : IEventBus
    {
        ///<inheritdoc/>
        public Dictionary<string, List<IIntegrationEventHandler>> Topics { get; }
            = new Dictionary<string, List<IIntegrationEventHandler>>();

        ///<inheritdoc/>
        public virtual void Subscribe(IIntegrationEventHandler handler, string topic = null)
        {
            var topicName = topic ?? handler.Topic;
            if (!Topics.ContainsKey(topicName))
            {
                Topics.Add(topicName, new List<IIntegrationEventHandler> { handler });
            }
            else if (Topics[topicName] != handler)
            {
                Topics[topicName].Add(handler);
            }
        }

        ///<inheritdoc/>
        public abstract Task PublishAsync<TIntegrationEvent>(TIntegrationEvent @event, string topic = null)
            where TIntegrationEvent : IIntegrationEvent;
    }
}
