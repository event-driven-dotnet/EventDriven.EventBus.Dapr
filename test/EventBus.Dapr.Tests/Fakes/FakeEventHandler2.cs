using EventBus.Abstractions;
using System.Threading.Tasks;

namespace EventBus.Dapr.Tests.Fakes
{
    public class FakeEventHandler2 : IntegrationEventHandler<FakeIntegrationEvent>
    {
        public FakeState State { get; }

        public FakeEventHandler2(FakeState state)
        {
            State = state;
        }

        public override Task HandleAsync(FakeIntegrationEvent @event)
        {
            // Mutate State Date
            State.Date = @event.CreationDate;
            return Task.CompletedTask;
        }
    }
}
