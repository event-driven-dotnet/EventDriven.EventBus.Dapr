using Common.Models;
using EventBus.Abstractions;
using System.Collections.Generic;

namespace Common.Events
{
    public record WeatherForecastGeneratedEvent(IEnumerable<WeatherForecast> WeatherForecasts) : IntegrationEvent;
}
