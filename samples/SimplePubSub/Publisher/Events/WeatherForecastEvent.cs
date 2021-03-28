using Publisher.Models;
using EventBus.Abstractions;
using System.Collections.Generic;

namespace Publisher.Events
{
    public record WeatherForecastEvent(IEnumerable<WeatherForecast> WeatherForecasts) : IntegrationEvent;
}
