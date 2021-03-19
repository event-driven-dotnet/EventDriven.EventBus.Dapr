using EventBus.Abstractions;
using System.Threading.Tasks;

namespace EventBus.Dapr.Tests.Fakes
{
    public class FakeEventHandler1 : IntegrationEventHandler<FakeIntegrationEvent>
    {
        public FakeState State { get; }

        public FakeEventHandler1(FakeState state)
        {
            State = state;
        }

        public override Task HandleAsync(FakeIntegrationEvent @event)
        {
            // Mutate State Data
            State.Data = @event.Data;
            return Task.CompletedTask;
        }
    }
}
