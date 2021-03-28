using EventBus.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventBus.Dapr.Tests.Fakes
{
    public class FakeMessageBroker
    {
        public Dictionary<string, List<IIntegrationEventHandler>> Topics { get; } = new();

        public void Subscribe(IIntegrationEventHandler handler, string topic = null)
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
