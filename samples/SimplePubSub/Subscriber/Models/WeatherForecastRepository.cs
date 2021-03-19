using System.Collections.Generic;

namespace Subscriber.Models
{
    public class WeatherForecastRepository
    {
        public IEnumerable<WeatherForecast> WeatherForecasts { get; set; } = new List<WeatherForecast>();
    }
}
