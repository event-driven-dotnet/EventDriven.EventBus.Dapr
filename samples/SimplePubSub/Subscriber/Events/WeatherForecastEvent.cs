using EventBus.Abstractions;
using Subscriber.Models;
using System.Collections.Generic;

namespace Subscriber.Events
{
    public record WeatherForecastEvent(IEnumerable<WeatherForecast> WeatherForecasts) : IntegrationEvent
    {
    }
}
