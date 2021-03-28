using EventBus.Abstractions;

namespace EventBus.Dapr.Tests.Fakes
{
    public record FakeIntegrationEvent(string Data) : IntegrationEvent;
}
