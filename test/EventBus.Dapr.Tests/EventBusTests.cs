using EventBus.Dapr.Tests.Fakes;
using System.Threading.Tasks;
using Xunit;

namespace EventBus.Dapr.Tests
{
    public class EventBusTests
    {
        [Fact]
        public async Task EventBus_Should_Invoke_Event_Handlers_Implicit_Topic()
        {
            // Create handlers
            var state = new FakeState { Data = "A" };
            var fakeHandler1 = new FakeEventHandler1(state);
            var fakeHandler2 = new FakeEventHandler2(state);

            // Create message broker
            var messageBroker = new FakeMessageBroker();
            messageBroker.Subscribe(fakeHandler1);
            messageBroker.Subscribe(fakeHandler2);

            // Create service bus
            var eventBus = new FakeEventBus(messageBroker);
            eventBus.Subscribe(fakeHandler1);
            eventBus.Subscribe(fakeHandler2);

            // Publish to service bus
            var @event = new FakeIntegrationEvent("B");
            await eventBus.PublishAsync(@event);

            // Assert
            Assert.Equal(@event.CreationDate, state.Date);
            Assert.Equal("B", state.Data);
        }

        [Fact]
        public async Task EventBus_Should_Invoke_Event_Handlers_Explicit_Topic()
        {
            // Create handlers
            var state = new FakeState { Data = "A" };
            var fakeHandler1 = new FakeEventHandler1(state);
            var fakeHandler2 = new FakeEventHandler2(state);

            // Create message broker
            var messageBroker = new FakeMessageBroker();
            messageBroker.Subscribe(fakeHandler1, "my-topic");
            messageBroker.Subscribe(fakeHandler2, "my-topic");

            // Create service bus
            var eventBus = new FakeEventBus(messageBroker);
            eventBus.Subscribe(fakeHandler1);
            eventBus.Subscribe(fakeHandler2);

            // Publish to service bus
            var @event = new FakeIntegrationEvent("B");
            await eventBus.PublishAsync(@event, "my-topic");

            // Assert
            Assert.Equal(@event.CreationDate, state.Date);
            Assert.Equal("B", state.Data);
        }
    }
}
