using EventBus.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventBus.Dapr.Tests.Fakes
{
    public class FakeMessageBroker
    {
        public Dictionary<string, List<IIntegrationEventHandler>> Topics { get; }
            = new Dictionary<string, List<IIntegrationEventHandler>>();

        public void Subscribe(IIntegrationEventHandler handler, string topic = null)
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

        public Task PublishEventAsync<TIntegrationEvent>(TIntegrationEvent @event, string topic)
            where TIntegrationEvent : IIntegrationEvent
        {
            var handlers = Topics[topic];
            if (handlers != null)
            {
                foreach (var handler in handlers)
                {
                    handler.HandleAsync(@event);
                }
            }
            return Task.CompletedTask;
        }
    }
}
