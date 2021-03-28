using EventBus.Abstractions;

namespace Common.Events
{
    public record WeatherForecastRequestedEvent : IntegrationEvent;
}
