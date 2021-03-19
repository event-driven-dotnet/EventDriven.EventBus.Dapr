using Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeatherGenerator.Factories
{
    public class WeatherFactory
    {
        private readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public async Task<IEnumerable<WeatherForecast>> CreateWeather(int delaySeconds)
        {
            // Simulate latency
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast(
                DateTime.Now.AddDays(index),
                rng.Next(-20, 55),
                Summaries[rng.Next(Summaries.Length)]
            ));
        }
    }
}
