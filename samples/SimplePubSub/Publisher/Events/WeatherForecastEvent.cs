using Publisher.Models;
using System.Collections.Generic;
using EventDriven.EventBus.Abstractions;

namespace Publisher.Events
{
    public record WeatherForecastEvent(IEnumerable<WeatherForecast> WeatherForecasts) : IntegrationEvent;
}
