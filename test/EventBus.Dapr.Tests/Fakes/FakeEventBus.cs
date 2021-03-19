using System.Threading.Tasks;

namespace EventBus.Dapr.Tests.Fakes
{
    public class FakeEventBus : Abstractions.EventBus
    {
        private readonly FakeMessageBroker _messageBroker;

        public FakeEventBus(FakeMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }

        public override async Task PublishAsync<TIntegrationEvent>(TIntegrationEvent @event, string topic = null)
        {
            var topicName = topic ?? @event.GetType().Name;
            await _messageBroker.PublishEventAsync(@event, topicName);
        }
    }
}
