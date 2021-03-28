using System;
using System.Collections.Generic;
using System.Linq;

namespace Publisher.Models
{
    public class WeatherFactory
    {
        private readonly string[] _summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public IEnumerable<WeatherForecast> CreateWeather()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast(
                DateTime.Now.AddDays(index),
                rng.Next(-20, 55),
                _summaries[rng.Next(_summaries.Length)]
            ));
        }
    }
}
