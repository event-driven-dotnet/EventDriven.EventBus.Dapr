using Common.Models;
using System.Collections.Generic;
using EventDriven.EventBus.Abstractions;

namespace Common.Events
{
    public record WeatherForecastGeneratedEvent(IEnumerable<WeatherForecast> WeatherForecasts) : IntegrationEvent;
}
