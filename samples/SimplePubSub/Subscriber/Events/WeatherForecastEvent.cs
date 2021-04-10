using Subscriber.Models;
using System.Collections.Generic;
using EventDriven.EventBus.Abstractions;

namespace Subscriber.Events
{
    public record WeatherForecastEvent(IEnumerable<WeatherForecast> WeatherForecasts) : IntegrationEvent;
}
